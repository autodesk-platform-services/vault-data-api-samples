import { createFileRoute, Outlet, Link, useRouterState, useNavigate } from '@tanstack/react-router'
import { Menu, Layout, Button } from 'antd';
const { Header, Sider, Content } = Layout;
import {
  LinkOutlined,
} from '@ant-design/icons';

import { useSnapshot } from 'valtio'
import { logout, changeServer } from '~/store/auth'
import { connectionStore } from '~/features/connection/connection'

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
  const navigate = useNavigate();
  const routerState = useRouterState();
  const { connection } = useSnapshot(connectionStore)
  

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
      <Button style={{ marginLeft: 'auto' }} onClick={() => { logout(); navigate({ to: '/' }); }}>Sign out</Button>
      <Button onClick={() => { changeServer(); navigate({ to: '/' }); }} title={connection?.serverAddress}>Change Server</Button>
      
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
