import { createFileRoute } from '@tanstack/react-router'
import { useEffect } from 'react'
import { Table } from 'antd';
import { useSnapshot } from 'valtio'
import { itemsStore, sync } from '~/store/itemsStore';

export const Route = createFileRoute('/_workspace/items')({
  component: Component
})

const columns = [
  {
    title: 'Name',
    dataIndex: 'name',
    fixed: true,
    width: 150,
ellipsis: true,
  },
  {
    title: 'Id',
    dataIndex: 'id',
    width: 150,
  ellipsis: true,
  },
  {
    title: 'Number',
    dataIndex: 'number',
    width: 150,
ellipsis: true,
  },
  {
    title: 'Title',
    dataIndex: 'title',
    width: 150,
ellipsis: true,
  },
  {
    title: 'Version',
    dataIndex: 'version',
    width: 150,
  ellipsis: true,
  },
  {
    title: 'Category',
    dataIndex: 'category',
    width: 150,
ellipsis: true,
  },
  {
    title: 'State',
    dataIndex: 'state',
    width: 150,
ellipsis: true,
  },
  {
    title: 'Revision',
    dataIndex: 'revision',
    width: 150,
ellipsis: true,
  },
  {
    title: 'Description',
    dataIndex: 'description',
    width: 150,
ellipsis: true,
  },
  {
    title: 'Read Only',
    dataIndex: 'isReadOnly',
    width: 150,
    ellipsis: true,
  },
  {
    title: 'Last Modified Date',
    dataIndex: 'lastModifiedDate',
    width: 150,
    ellipsis: true,
  },
];

function Component() {
  const {items, loading, doneSync} = useSnapshot(itemsStore)

  useEffect(()=>{
    if(!doneSync && !loading){
      sync()
    }
  }, [loading, doneSync])

  return <Table
  pagination={false} 
  rowKey="id"
  scroll={{  y: 600 }}
  columns={columns} 
  dataSource={items} 
  size="small"
  bordered={true}
  />
}