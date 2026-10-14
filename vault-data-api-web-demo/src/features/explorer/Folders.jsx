import {useEffect, useState} from 'react'
import { useNavigate } from '@tanstack/react-router'
import { useSnapshot } from 'valtio'
import { filesStore } from '~/features/explorer/filesStore'
import { resolveFolderId } from '~/features/explorer/folderTree'
import { Table } from 'antd';

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
export default function Component({id='root'}){

    const navigate = useNavigate()
    const { doneSync, folders, rootFolder, rootLoading, rootError, directoryStates } = useSnapshot(filesStore)
    const folderId = resolveFolderId(id, rootFolder)
    const folderState = directoryStates[String(folderId)]
    const [data, setData] = useState([])
    
    useEffect(()=>{
        if(doneSync){
          const fl = folders
          .params({ folderId })
          .filter((d, $) => d.parentId == $.folderId)
          .objects()
          setData(fl)
        }
    }, [doneSync, folderId, folders])

    return <Table
      loading={rootLoading || !!(folderId && !rootError && !folderState?.error && (folderState?.loading || !folderState?.loaded))}
      pagination={false} 
      rowKey="id"
      scroll={{ x: "max-content", y: 600 }}
      columns={columns} 
      dataSource={data} 
      size="small"
      bordered={true}
      onRow={(record) => {
        return {
          onClick: () => {
            navigate({
              to: '/explorer/$id',
              params: { id: record.id } 
            })
          },
        };
      }}
      />
}
