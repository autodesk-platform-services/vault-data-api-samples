import { useState, useEffect } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Alert, Button, Form, Input, Select, Space, Tabs, Typography } from 'antd';
import { useSnapshot } from 'valtio';
import { authStore, listVaults, login, logout, selectAutodeskVault, signInAutodesk } from '~/features/authentication/authStore';
import { changeServer } from '~/features/authentication/authStore';
import { connectionStore, connectServer } from '~/features/connection/connection';

export default function Login() {
  const navigate = useNavigate();
  const { oauth, error: authError } = useSnapshot(authStore);
  const { connection, connecting, error: connectionError } = useSnapshot(connectionStore);
  const [serverAddress, setServerAddress] = useState(() => localStorage.getItem('vault:last-server') || '');
  const [vaults, setVaults] = useState([]);
  const [busy, setBusy] = useState(false);
  const [loadingVaults, setLoadingVaults] = useState(false);
  const [error, setError] = useState('');
  const [loadVersion, setLoadVersion] = useState(0);
  const [clientId, setClientId] = useState(() => localStorage.getItem('vault:aps:clientId') || '');

  useEffect(() => {
    if (!connection) {
      setVaults([]);
      return;
    }
    let active = true;
    setVaults([]);
    setLoadingVaults(true);
    listVaults().then(data => {
      if (active) {
        setVaults(data.results || []);
      }
    }).catch(error => {
      if (active) {
        setError(error.message);
      }
    }).finally(() => {
      if (active) {
        setLoadingVaults(false);
      }
    });
    return () => { active = false; };
  }, [oauth?.accessToken, loadVersion, connection]);

  async function run(action) {
    setBusy(true);
    setError('');
    try {
      await action();
      if (authStore.session) {
        navigate({ to: '/' });
      }
    } catch (error) {
      setError(error.message);
    } finally {
      setBusy(false);
    }
  }
  const options = vaults.map(vault => ({ value: String(vault.id), label: vault.name }));
  const vaultSelect = <Form.Item name="vaultId" rules={[{ required: true, message: 'Select a Vault.' }]}>
    <Select placeholder="Select Vault" loading={loadingVaults} options={options} disabled={busy} />
  </Form.Item>;
  const vaultLogin = <Form layout="vertical" onFinish={values => run(() => {
    const vault = vaults.find(v => String(v.id) === values.vaultId);
    if (!vault) {
      throw new Error('Select an available Vault.');
    }
    return login({ ...values, vault: vault.name });
  })}>
    {vaultSelect}
    <Form.Item name="userName" rules={[{ required: true, message: 'Enter your username.' }]}><Input placeholder="Username" disabled={busy} /></Form.Item>
    <Form.Item name="password"><Input.Password placeholder="Password" disabled={busy} /></Form.Item>
    <Button type="primary" htmlType="submit" loading={busy}>Login</Button>
  </Form>;
  const autodeskLogin = <Space direction="vertical" style={{ width: '100%' }}>
    <Typography.Text>Sign in with your Autodesk ID to access the Vault Gateway.</Typography.Text>
    <Input aria-label="APS Client ID" placeholder="APS Client ID" value={clientId} disabled={busy}
      onChange={event => setClientId(event.target.value)} />
    <Typography.Text type="secondary">Registered callback URL: {window.location.origin}/</Typography.Text>
    <Button type="primary" loading={busy} disabled={!clientId.trim()} onClick={() => run(() => signInAutodesk(clientId))}>Sign in with Autodesk ID</Button>
  </Space>;

  return <Space direction="vertical" style={{ width: 400 }} size="middle">
    <img src="/vault-logo.png" alt="Vault" style={{ maxWidth: 200 }} />
    {(error || authError || connectionError) && <Alert type="error" showIcon message={error || authError || connectionError} />}
    {!connection ? <Space direction="vertical" style={{ width: '100%' }}>
      <Input aria-label="Server Address" placeholder="Server Address (http://server or https://gateway)"
        value={serverAddress} disabled={connecting} onChange={event => setServerAddress(event.target.value)}
        onPressEnter={() => { if (serverAddress.trim() && !connecting) run(() => connectServer(serverAddress)); }} />
      <Button type="primary" loading={connecting} disabled={!serverAddress.trim()} onClick={() => run(() => connectServer(serverAddress))}>Connect</Button>
    </Space> : <>
    <Typography.Text>Connected to {connection.serverAddress}</Typography.Text>
    <Button disabled={busy} onClick={() => { changeServer(); setError(''); navigate({ to: '/' }); }}>Change Server</Button>
    {oauth ? <>
      <Alert type="success" message="Autodesk ID signed in. Select a Vault to continue." />
      <Form layout="vertical" onFinish={values => run(() => selectAutodeskVault(vaults.find(v => String(v.id) === values.vaultId)))}>
        {vaultSelect}
        <Button type="primary" htmlType="submit" loading={busy}>Open Vault</Button>
      </Form>
      <Button disabled={busy} onClick={logout}>Use another account</Button>
    </> : <Tabs defaultActiveKey="autodesk" items={[
      { key: 'autodesk', label: 'Autodesk ID', children: autodeskLogin },
      { key: 'vault', label: 'Vault username', children: vaultLogin },
    ]} />}
    <Button size="small" disabled={busy || loadingVaults} onClick={() => { setError(''); setLoadVersion(v => v + 1); }}>Reload Vaults</Button>
    </>}
  </Space>;
}
