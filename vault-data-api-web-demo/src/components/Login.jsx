import { useState, useEffect } from 'react';
import { Link } from '@tanstack/react-router'
import { Button, Form, Input, Select } from 'antd';
import Apis from '~/api'
import { useSnapshot } from 'valtio'
import { authStore, login } from '~/store/auth'
const Login = () => {
    const {session} = useSnapshot(authStore)
    const [vaults, setVaults] = useState([])
    const [form] = Form.useForm();

    useEffect(() => {
      const fn = async ()=>{
        if(!session){
          const response = await Apis.global.getVaults({})
          setVaults(response.results)
        }
      }
      fn()
    }, [session])

    const handleLogin = async (values) => {
      await login(values)
    }

    return (
        <>
            <Link to="/">
              <img src="./vault-logo.png" />
            </Link>  
            <Form
              layout= "vertical"
              onFinish={handleLogin}
              form={form}
              style={{
                width: 320,
                marginTop:'32px',
              }}
            >
              <Form.Item name="vault" >
                <Select placeholder="Select Vault">
                  {
                    vaults.map(v => (<Select.Option value={v.name} key={v.id}>{v.name}</Select.Option>))
                  }
                </Select>
              </Form.Item>
              <Form.Item name="userName">
                <Input placeholder="Username"/>
              </Form.Item>
              <Form.Item name="password">
                <Input placeholder="Password" type="password" />
              </Form.Item>
              <Form.Item>
                <Button type="primary"  htmlType="submit">Login</Button>
              </Form.Item>
            </Form>
        </>
    );
  };
  
  export default Login;

