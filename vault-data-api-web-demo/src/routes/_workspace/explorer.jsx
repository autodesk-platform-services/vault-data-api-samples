import { createFileRoute, Outlet, Link, useNavigate, useLocation } from '@tanstack/react-router'
import { Tabs } from 'antd';
import { useEffect } from 'react'
import { useSnapshot } from 'valtio'
import { filesStore, sync, explorer } from '~/store/filesStore';
import Split from 'react-split'
import { Tree } from 'antd';
const { DirectoryTree } = Tree;

export const Route = createFileRoute('/_workspace/explorer')({
  component: Layout
})

function Layout () {

  const navigate = useNavigate()
  const {loading, doneSync} = useSnapshot(filesStore)
  const {id} = Route.useParams()
  const { pathname } = useLocation()
  const fromPath = id ? Route.fullPath + '/'+ '$id' : Route.fullPath
  let selectedTab = pathname.split('/').reverse().shift()
  switch (selectedTab){
    case 'folders':
    case 'files':
    case 'links':
      break
    default:
      selectedTab = 'folders' 
  }
  // console.log(selectedTab)
  useEffect(()=>{
    if(!doneSync && !loading){
      sync()
    }
  }, [loading, doneSync])
  
  const items = [
    {
      key: 'folders',
      label: <Link from={fromPath} to="./folders">Folders</Link>,
      children: <Outlet />,
    },
    {
      key: 'files',
      label: <Link from={fromPath} to="./files">Files</Link>,
      children: <Outlet />,
    },
    {
      key: 'links',
      label: <Link from={fromPath} to="./links">Links</Link>,
      children: <Outlet />,
    },
  ];
  const onSelect = (selectedKeys, info) => {
    navigate({
      to: '/explorer/$id',
      params: { id: info.node.id } 
    })
  };

  return <Split
            className="split"
            sizes={[25, 75]}
            expandToMin={true}
            gutterSize={10}
            snapOffset={30}
            dragInterval={1}
            direction="horizontal"
            cursor="col-resize"
            gutterAlign="start"
          >
            <div style={{backgroundColor: '#fff', height: '100%', overflow: 'auto'}}>
            <DirectoryTree
              multiple
              onSelect={onSelect}
              treeData={explorer.tree}
            />
            </div>
            <div style={{paddingLeft: '8px'}}>
              <Tabs items={items} activeKey={selectedTab}/>
            </div>
          </Split>
}