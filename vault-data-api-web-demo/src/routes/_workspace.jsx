import { createFileRoute, Outlet, Link, useRouterState } from '@tanstack/react-router'
import { Menu, Layout, Button } from 'antd';
const { Header, Sider, Content } = Layout;
import {
  LinkOutlined,
} from '@ant-design/icons';

import { useSnapshot } from 'valtio'
import { filesStore, sync as filesSync } from '~/store/filesStore'

const headerStyle  = {
  height: 60,
  paddingInline: 16,
  display: 'flex',
  alignItems: 'center',
};

const contentStyle = {
  padding: '0 24px',
  overflowY: 'auto',
  flexGrow: 1,
  display: 'flex',
  flexDirection: 'column',
};

const siderStyle = {
};

const layoutStyle = {
  height: '100%',
  display: 'flex',
  flexDirection: 'column',
};

const layoutStyle1 = {
  flexGrow: '1',
  display: 'flex',
  flexDirection: 'row',
};

const Layoutt = () => {  
  const routerState = useRouterState();
  const {loading, doneSync} = useSnapshot(filesStore)
  
  const handleSync = () => {
    filesSync()
  }

  const items = [
    {icon: <LinkOutlined />, label : <Link to="/explorer">Explorer</Link>, key: '/explorer'},
    {icon: <LinkOutlined />, label : <Link to="/items">Items</Link>, key: '/items'},
    {icon: <LinkOutlined />, label : <Link to="/changeorders">Change Orders</Link>, key: '/changeorders'},
    {icon: <LinkOutlined />, label : <Link to="/playground">API Playground</Link>, key: '/playground'}
  ]
  return <Layout style={layoutStyle}>
    <Header style={headerStyle}>
      <Link to="/">
        <img src="./vault-logo-light.png" />
      </Link>
      {
        !doneSync && <Button type="primary" loading={loading} style={{marginLeft: 'auto'}} onClick={handleSync} >Sync</Button>
      }
      
      </Header>
    <Layout style={layoutStyle1}>
      <Sider width="256px" style={siderStyle}>
        <Menu
          style={{
            width: 256,
            height: '100%',
          }}
          selectedKeys={[routerState.location.pathname]}
          mode="inline"
          theme="light"
          items={items}
        />
      </Sider>
      <Content style={contentStyle}><Outlet /></Content>
    </Layout>
  </Layout>
}

export const Route = createFileRoute('/_workspace')({
  component: Layoutt
})