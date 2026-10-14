import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { useSnapshot } from 'valtio'
import { Alert, Button, Checkbox, Form, Input, Modal, Select, Space, Table, message } from 'antd'
import { VAULT_API_BASE_URL } from '~/api'
import { authStore } from '~/features/authentication/authStore'
import { filesStore, sync } from '~/features/explorer/filesStore'
import FileUploadDialog from '~/features/fileUpload/FileUploadDialog'
import { createFileUploadService } from '~/features/fileUpload/fileUploadService'
import { resolveFolderId } from '~/features/explorer/folderTree'
import { CANONICAL_API_BASE, connectionFetch } from '~/features/connection/connection'
import {
  fetchLifecycleDefinitions,
  updateFileLifecycleDefinitions,
  updateFileLifecycleStates,
} from '~/features/explorer/lifecycleApi'

const columns = [
  {
    title: 'Name',
    dataIndex: 'name',
    fixed: true,
    width: 250,
ellipsis: true,
  },
  {
    title: 'Id',
    dataIndex: 'id',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Version',
    dataIndex: 'version',
    width: 100,
    ellipsis: true,
  },
  {
    title: 'Parent Id',
    dataIndex: 'parentId',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Full Name',
    dataIndex: 'fullName',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Revision',
    dataIndex: 'revision',
    width: 180,
ellipsis: true,
  },
  {
    title: 'Category',
    dataIndex: 'category',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Category Color',
    dataIndex: 'categoryColor',
    width: 250,
ellipsis: true,
  },
  {
    title: 'State',
    dataIndex: 'state',
    width: 220,
ellipsis: true,
  },
  {
    title: 'State Color',
    dataIndex: 'stateColor',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Sub Folder Count',
    dataIndex: 'subfolderCount',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Is Library',
    dataIndex: 'isLibrary',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Is Cloaked',
    dataIndex: 'isCloaked',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Is Read Only',
    dataIndex: 'isReadOnly',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Create Date',
    dataIndex: 'createDate',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Create User Name',
    dataIndex: 'createUserName',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Entity Type',
    dataIndex: 'entityType',
    width: 250,
ellipsis: true,
  },
  {
    title: 'Url',
    dataIndex: 'url',
    width: 250,
ellipsis: true,
    
  },
];


export default function Component({id='root'}) {
    
    const navigate = useNavigate()
    const [modal, modalContextHolder] = Modal.useModal()
    const [messageApi, messageContextHolder] = message.useMessage()
    const { session } = useSnapshot(authStore)
    const { doneSync, files, folders, rootFolder, rootLoading, rootError, directoryStates } = useSnapshot(filesStore)
    const [needsRefresh, setNeedsRefresh] = useState(false)
    const folderId = resolveFolderId(id, rootFolder)
    const folderState = directoryStates[String(folderId)]
    const loading = !!folderState?.loading
    const [uploadTarget, setUploadTarget] = useState(null)
    const [isFolderLoading, setIsFolderLoading] = useState(false)
    const currentFolder = doneSync ? folders.objects().find((folder) => String(folder.id) === String(folderId)) : null
    const [form] = Form.useForm()
    const [tableData, setTableData] = useState([])
    const [selectedFileKeys, setSelectedFileKeys] = useState([])
    const [selectedFile, setSelectedFile] = useState(null)
    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const [isDefinitionsLoading, setIsDefinitionsLoading] = useState(false)
    const [isSubmitting, setIsSubmitting] = useState(false)
    const [availableLifecycleDefinitions, setAvailableLifecycleDefinitions] = useState([])
    useEffect(() => {
      setNeedsRefresh(false)
    }, [folderId, folderState?.version])
    
    useEffect(()=>{
        if(doneSync){
          const fs = files
            .params({parentId: folderId})
            .filter((d) => d.parentId == parentId)
            .objects()
          setTableData(fs)
        }
    }, [doneSync, folderId, files])

    useEffect(() => {
      setSelectedFileKeys([])
      setSelectedFile(null)
    }, [id])

    const toOption = (value, label) => ({
      value: String(value || ''),
      label: String(label || value || ''),
    })

    const definitionOptions = useMemo(() => {
      const optionsMap = new Map()
      availableLifecycleDefinitions.forEach((definition) => {
        if(!definition?.url){
          return
        }
        optionsMap.set(definition.url, toOption(definition.url, definition.displayName || definition.name || definition.url))
      })
      tableData.forEach((row) => {
        if(!row.lifecycleDefinitionUrl || optionsMap.has(row.lifecycleDefinitionUrl)){
          return
        }
        optionsMap.set(
          row.lifecycleDefinitionUrl,
          toOption(row.lifecycleDefinitionUrl, row.lifecycleDefinitionName || row.lifecycleDefinitionUrl)
        )
      })
      return Array.from(optionsMap.values())
    }, [availableLifecycleDefinitions, tableData])

    const lifecycleDefinitionUrl = Form.useWatch('lifecycleDefinitionUrl', form)
    const selectedDefinition = useMemo(() => {
      if(!lifecycleDefinitionUrl){
        return null
      }
      return availableLifecycleDefinitions.find((item) => item?.url === lifecycleDefinitionUrl) || null
    }, [availableLifecycleDefinitions, lifecycleDefinitionUrl])

    const stateOptions = useMemo(() => {
      const optionsMap = new Map()
      const definitionStates = Array.isArray(selectedDefinition?.states) ? selectedDefinition.states : []
      definitionStates.forEach((state) => {
        if(!state?.url){
          return
        }
        optionsMap.set(state.url, toOption(state.url, state.displayName || state.name || state.url))
      })
      if(optionsMap.size > 0){
        return Array.from(optionsMap.values())
      }
      if(selectedFile?.lifecycleStateUrl){
        optionsMap.set(
          selectedFile.lifecycleStateUrl,
          toOption(
            selectedFile.lifecycleStateUrl,
            selectedFile.lifecycleStateName || selectedFile.state || selectedFile.lifecycleStateUrl
          )
        )
      }
      return Array.from(optionsMap.values())
    }, [selectedDefinition, selectedFile])

    const currentStateUrl = Form.useWatch('lifecycleStateUrl', form)
    useEffect(() => {
      if(!currentStateUrl){
        return
      }
      const exists = stateOptions.some((option) => option.value === currentStateUrl)
      if(!exists){
        form.setFieldValue('lifecycleStateUrl', undefined)
      }
    }, [currentStateUrl, stateOptions, form])

    const openDialog = async () => {
      if(!selectedFile){
        messageApi.warning('Please select one file.')
        return
      }
      form.setFieldsValue({
        lifecycleDefinitionUrl: selectedFile.lifecycleDefinitionUrl || undefined,
        lifecycleStateUrl: selectedFile.lifecycleStateUrl || undefined,
        specifyRevision: false,
        revision: '',
        comment: '',
      })
      setIsDialogOpen(true)
      if(availableLifecycleDefinitions.length === 0 && session?.vaultId && session?.token){
        try{
          setIsDefinitionsLoading(true)
          const defs = await fetchLifecycleDefinitions(session)
          setAvailableLifecycleDefinitions(defs)
        }catch(error){
          messageApi.error(error?.message || 'Failed to load lifecycle definitions.')
        }finally{
          setIsDefinitionsLoading(false)
        }
      }
    }

    const getLifecycleDefinitionIdentity = (value) => {
      const text = String(value || '').trim()
      if(!text){
        return ''
      }
      const urlMatch = text.match(/\/lifecycle-definitions\/([^/?#]+)/i)
      if(urlMatch && urlMatch[1]){
        return urlMatch[1].toLowerCase()
      }
      return text.toLowerCase()
    }

    const onSubmit = async () => {
      if(!session?.vaultId || !session?.token){
        messageApi.error('No active Vault session.')
        return
      }
      try{
        const values = await form.validateFields()
        if(!selectedFile){
          throw new Error('Please select one file.')
        }
        const targetDefinition = getLifecycleDefinitionIdentity(values.lifecycleDefinitionUrl)
        const specifyRevision = !!values.specifyRevision
        if(!selectedFile.masterId){
          throw new Error(`File ${selectedFile.name || selectedFile.id} is missing master id.`)
        }
        const currentDefinition = getLifecycleDefinitionIdentity(
          selectedFile.lifecycleDefinitionUrl || selectedFile.lifecycleDefinitionName
        )
        const changeLifecycleDefinition =
          !!targetDefinition && (!currentDefinition || currentDefinition !== targetDefinition)
        if(specifyRevision && !changeLifecycleDefinition && !values.revision){
          throw new Error('Revision is required when Specify Revision is enabled and lifecycle definition is not changed.')
        }
        const request = {
          entityUrl: `${CANONICAL_API_BASE}/vaults/${session.vaultId}/files/${selectedFile.masterId}`,
          lifecycleStateUrl: values.lifecycleStateUrl,
        }
        if(changeLifecycleDefinition){
          request.lifecycleDefinitionUrl = values.lifecycleDefinitionUrl
        }
        if(specifyRevision && !changeLifecycleDefinition){
          request.revision = values.revision
        }

        setIsSubmitting(true)
        if(changeLifecycleDefinition){
          await updateFileLifecycleDefinitions(session, {
            updateLifecycleDefinitionRequests: [
              {
                entityUrl: request.entityUrl,
                lifecycleDefinitionUrl: request.lifecycleDefinitionUrl,
                lifecycleStateUrl: request.lifecycleStateUrl,
              },
            ],
            comment: values.comment,
          })
        }else{
          await updateFileLifecycleStates(session, {
            updateLifecycleStateRequests: [request],
            comment: values.comment,
          })
        }
        messageApi.success('Updated lifecycle state for selected file.')
        setIsDialogOpen(false)
        setSelectedFileKeys([])
        setSelectedFile(null)
        await sync(folderId)
      }catch(error){
        if(error?.errorFields){
          return
        }
        modal.error({
          title: 'Vault API Error',
          content: error?.message || 'Failed to update lifecycle state.',
        })
      }finally{
        setIsSubmitting(false)
      }
    }
    
    const openAddFile = async () => {
      setIsFolderLoading(true)
      const operationSession = { ...session }
      try {
        let folder = currentFolder ? { ...currentFolder } : null
        if (!folder) {
          const service = createFileUploadService({ baseURL: VAULT_API_BASE_URL, session: operationSession, fetcher: connectionFetch })
          folder = await service.getFolder(folderId || 'root')
        }
        if (folder.isReadOnly || folder.isCloaked) {
          throw new Error('The selected folder is not writable.')
        }
        setUploadTarget({ mode: 'add', folder, session: operationSession })
      } catch (error) {
        messageApi.error(error.message)
      } finally {
        setIsFolderLoading(false)
      }
    }

    const refreshFiles = async () => {
      try {
        await sync(folderId)
        setSelectedFileKeys([])
        setSelectedFile(null)
      } catch (error) {
        messageApi.error(error.message || 'Could not refresh the file list.')
      }
    }

    return <>
      {modalContextHolder}
      {messageContextHolder}
      <Space style={{ marginBottom: 8 }}>
        <Button loading={loading} disabled={!folderId || !!uploadTarget || isSubmitting || isFolderLoading} onClick={refreshFiles}>
          Refresh
        </Button>
        <Button loading={isFolderLoading} disabled={!doneSync || !folderId || loading || currentFolder?.isReadOnly || currentFolder?.isCloaked || !!uploadTarget}
          onClick={openAddFile}>
          Add File
        </Button>
        <Button disabled={loading || !selectedFile?.masterId || selectedFile.isReadOnly || selectedFile.isCloaked || !!uploadTarget}
          onClick={() => setUploadTarget({ mode: 'update', file: { ...selectedFile }, session: { ...session } })}>
          Update File
        </Button>
        <Button type="primary" disabled={loading || selectedFileKeys.length === 0} onClick={openDialog}>
          Change State
        </Button>
      </Space>
      {needsRefresh && <Alert type="info" showIcon style={{ marginBottom: 8 }}
        message="The file list may be outdated. Select Refresh to load the current Vault state." />}
      {uploadTarget && <FileUploadDialog target={uploadTarget} session={uploadTarget.session}
        onClose={() => setUploadTarget(null)} onChanged={() => {
          setNeedsRefresh(true)
          setSelectedFileKeys([])
          setSelectedFile(null)
        }} />}
      <Table
        loading={rootLoading || !!(folderId && !rootError && !folderState?.error && (loading || !folderState?.loaded))}
        pagination={false} 
        rowKey="id"
        scroll={{ x: "max-content", y: 600 }}
        columns={columns} 
        dataSource={tableData} 
        size="small"
        bordered={true}
        rowSelection={{
          type: 'radio',
          selectedRowKeys: selectedFileKeys,
          onChange: (keys, rows) => {
            setSelectedFileKeys(keys)
            setSelectedFile(rows?.[0] || null)
          },
        }}
        onRow={(record) => {
          return {
            onClick: () => {
              navigate({
                to: '/explorer/files/$id',
                params: { id: record.id } 
              })
            },
          };
        }}
      />
      <Modal
        title={selectedFile ? `Change State - '${selectedFile.name || ''}'` : 'Change State'}
        open={isDialogOpen}
        onCancel={() => setIsDialogOpen(false)}
        onOk={onSubmit}
        confirmLoading={isSubmitting}
        okText="OK"
        destroyOnClose
      >
        <Form
          layout="vertical"
          form={form}
          initialValues={{
            lifecycleDefinitionUrl: undefined,
            lifecycleStateUrl: undefined,
            specifyRevision: false,
            revision: '',
            comment: '',
          }}
        >
          <Form.Item
            label="Lifecycle Definition"
            name="lifecycleDefinitionUrl"
          >
            <Select
              showSearch
              allowClear
              loading={isDefinitionsLoading}
              options={definitionOptions}
              placeholder="Select lifecycle definition"
              optionFilterProp="label"
            />
          </Form.Item>
          <Form.Item
            label="Lifecycle State"
            name="lifecycleStateUrl"
            rules={[{ required: true, message: 'Please select lifecycle state.' }]}
          >
            <Select
              showSearch
              options={stateOptions}
              placeholder="Select lifecycle state"
              optionFilterProp="label"
            />
          </Form.Item>
          <Form.Item name="specifyRevision" valuePropName="checked">
            <Checkbox>Specify Revision</Checkbox>
          </Form.Item>
          <Form.Item
            noStyle
            shouldUpdate={(prev, curr) => prev.specifyRevision !== curr.specifyRevision || prev.lifecycleDefinitionUrl !== curr.lifecycleDefinitionUrl}
          >
            {({ getFieldValue }) => {
              const enabled = !!getFieldValue('specifyRevision')
              const selectedDefinitionValue = getLifecycleDefinitionIdentity(getFieldValue('lifecycleDefinitionUrl'))
              const currentDefinition = getLifecycleDefinitionIdentity(
                selectedFile?.lifecycleDefinitionUrl || selectedFile?.lifecycleDefinitionName
              )
              const hasSameDefinition =
                !!selectedDefinitionValue && !!currentDefinition && currentDefinition === selectedDefinitionValue
              const revisionRequired = enabled && (selectedDefinitionValue === '' || hasSameDefinition)
              return (
                <Form.Item
                  label="Revision"
                  name="revision"
                  rules={revisionRequired ? [{ required: true, message: 'Revision is required.' }] : []}
                >
                  <Input placeholder="Enter revision value" disabled={!enabled} />
                </Form.Item>
              )
            }}
          </Form.Item>
          <Form.Item label="Comment" name="comment">
            <Input.TextArea rows={3} placeholder="Enter comments" />
          </Form.Item>
        </Form>
      </Modal>
    </>
}
