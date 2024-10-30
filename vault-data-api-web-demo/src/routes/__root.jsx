import { Outlet, createRootRoute } from '@tanstack/react-router'
import { Flex, Spin } from 'antd';
import { useSnapshot } from 'valtio'
import { authStore, AuthStatus } from '~/store/auth'
import Login from '~/components/Login'

const boxStyle = {
    height: '100%'
};
  
const RootComponent = () => {

    const {authStatus, session, user} = useSnapshot(authStore)
    
    const wrap = (children) => (<Flex style={boxStyle} justify="center" align="center" vertical>
        {children}
    </Flex>)

    if(!session && !user){
        return wrap(<Login />)
    }

    if(authStatus == AuthStatus.PENDING){
        return wrap(<Spin size="large" />)
    }
    
    return <Outlet />
}

export const Route = createRootRoute({
  component: RootComponent
})

