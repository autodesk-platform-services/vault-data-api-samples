import { useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { Table } from 'antd';
import { useSnapshot } from 'valtio'
import { changeOrdersStore, sync } from '~/store/changeOrdersStore';

export const Route = createFileRoute('/_workspace/changeorders')({
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
    title: 'State',
    dataIndex: 'state',
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
    title: 'Assignees',
    dataIndex: 'assignees',
    width: 150,
ellipsis: true,
  },
  {
    title: '# Attchements',
    dataIndex: 'numberOfAttachments',
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
  const {changeOrders, loading, doneSync} = useSnapshot(changeOrdersStore)
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
  dataSource={changeOrders} 
  size="small"
  bordered={true}
  />
}