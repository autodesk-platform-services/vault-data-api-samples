import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { useSnapshot } from 'valtio'
import { Button, Checkbox, Form, Input, Modal, Select, Space, Table, message } from 'antd'
import { VAULT_API_BASE_URL } from '~/api'
import { authStore } from '~/store/auth'
import { filesStore, sync } from '~/store/filesStore'
import {
  fetchLifecycleDefinitions,
  updateFileLifecycleDefinitions,
  updateFileLifecycleStates,
} from '~/store/helper'

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


export default function Component({id='1'}) {
    
    const navigate = useNavigate()
    const [modal, modalContextHolder] = Modal.useModal()
    const [messageApi, messageContextHolder] = message.useMessage()
    const { session } = useSnapshot(authStore)
    const { doneSync, files } = useSnapshot(filesStore)
    const [form] = Form.useForm()
    const [tableData, setTableData] = useState([])
    const [selectedFileKeys, setSelectedFileKeys] = useState([])
    const [selectedFile, setSelectedFile] = useState(null)
    const [isDialogOpen, setIsDialogOpen] = useState(false)
    const [isDefinitionsLoading, setIsDefinitionsLoading] = useState(false)
    const [isSubmitting, setIsSubmitting] = useState(false)
    const [availableLifecycleDefinitions, setAvailableLifecycleDefinitions] = useState([])
    
    useEffect(()=>{
        if(doneSync){
          const fs = files
            .params({parentId: id})
            .filter((d) => d.parentId == parentId)
            .objects()
          setTableData(fs)
        }
    }, [doneSync, id, files])

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
          entityUrl: `${VAULT_API_BASE_URL}/vaults/${session.vaultId}/files/${selectedFile.masterId}`,
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
        await sync()
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
    
    return <>
      {modalContextHolder}
      {messageContextHolder}
      <Space style={{ marginBottom: 8 }}>
        <Button type="primary" disabled={selectedFileKeys.length === 0} onClick={openDialog}>
          Change State
        </Button>
      </Space>
      <Table
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