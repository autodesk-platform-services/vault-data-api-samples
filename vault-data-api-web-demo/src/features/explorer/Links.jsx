import {useEffect, useState} from 'react'
import { useSnapshot } from 'valtio'
import { filesStore } from '~/features/explorer/filesStore'
import { resolveFolderId } from '~/features/explorer/folderTree'
import { Table } from 'antd';

const columns = [
  {
    title: 'Name',
    dataIndex: 'name',
    fixed: true,
  },
  {
    title: 'Id',
    dataIndex: 'id',
    width: 200,
  },
  {
    title: 'Entity Type',
    dataIndex: 'entityType',
    width: 200,
  },
  {
    title: 'From Entity Id',
    dataIndex: 'fromEntityId',
    width: 200,
  },
  {
    title: 'To Entity Id',
    dataIndex: 'toEntityId',
    width: 200,
  },
  {
    title: 'Create Date',
    dataIndex: 'createDate',
    width: 200,
  },
  {
    title: 'Create User Name',
    dataIndex: 'createUserName',
    width: 200,
  },
];

const linkTypes = ['FileVersionLink', 'FolderLink', 'ChangeOrderLink', 'ItemVersionLink']
export default function Component({id='root'}){

    const { doneSync, links, rootFolder, rootLoading, rootError, directoryStates } = useSnapshot(filesStore)
    const folderId = resolveFolderId(id, rootFolder)
    const folderState = directoryStates[String(folderId)]
    const [data, setData] = useState([])
    
    useEffect(()=>{
        if(doneSync){
          const fl = links
          .params({fromEntityId: folderId})
          .filter((d) => d.fromEntityId == fromEntityId)
          .objects()
          setData(fl)
        }
    }, [doneSync, folderId, links])

  return <Table
    loading={rootLoading || !!(folderId && !rootError && !folderState?.error && (folderState?.loading || !folderState?.loaded))}
    pagination={false} 
    rowKey="id"
    scroll={{ x: "max-content", y: 600 }}
    columns={columns} 
    dataSource={data} 
    size="small"
    bordered={true}
  />
}
