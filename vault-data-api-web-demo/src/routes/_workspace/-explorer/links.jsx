import {useEffect, useState} from 'react'
import { useSnapshot } from 'valtio'
import { filesStore } from '~/store/filesStore'
import { Table } from 'antd';
import * as aq from 'arquero';

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
export default function Component({id='1'}){

    const { doneSync, links } = useSnapshot(filesStore)
    const [data, setData] = useState([])
    
    useEffect(()=>{
        if(doneSync){
          const fl = links
          .params({fromEntityId: id})
          .filter((d) => d.fromEntityId == fromEntityId)
          .objects()
          setData(fl)
        }
    }, [doneSync, id, links])

  return <Table
    pagination={false} 
    rowKey="id"
    scroll={{ x: "max-content", y: 600 }}
    columns={columns} 
    dataSource={data} 
    size="small"
    bordered={true}
  />
}