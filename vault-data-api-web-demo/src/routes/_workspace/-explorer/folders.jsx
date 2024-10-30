import {useEffect, useState} from 'react'
import { useNavigate } from '@tanstack/react-router'
import { useSnapshot } from 'valtio'
import { filesStore } from '~/store/filesStore'
import { Table } from 'antd';
import * as aq from 'arquero';

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
export default function Component({id='1'}){

    const navigate = useNavigate()
    const { doneSync, folders } = useSnapshot(filesStore)
    const [data, setData] = useState([])
    
    useEffect(()=>{
        if(doneSync){
          const fl = folders
          .params({parentId: id})
          .filter((d) => d.parentId == parentId)
          .objects()
          setData(fl)
        }
    }, [doneSync, id, folders])

    return <Table
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