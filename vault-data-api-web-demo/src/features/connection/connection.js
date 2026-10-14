import { proxy } from 'valtio';
export const CANONICAL_API_BASE = '/AutodeskDM/Services/api/vault/v2';
const key = 'vault:connection';
let controller = new AbortController();
let configured = () => {};
export const connectionStore = proxy({ connection: null, connecting: false, error: '' });

export function onConnectionConfigured(callback) {
  configured = callback;
}
export function getConnectionSignal() {
  return controller.signal;
}
export function connectionBase(connection = connectionStore.connection) {
  return connection ? `/__vault/${connection.connectionId}${CANONICAL_API_BASE}` : `/__vault/disconnected${CANONICAL_API_BASE}`;
}
export function disconnectServer() {
  controller.abort(new Error('The server connection changed.'));
  controller = new AbortController();
  connectionStore.connection = null;
  sessionStorage.removeItem(key);
  configured(null);
}

export async function connectServer(serverAddress, connectionId) {
  disconnectServer();
  connectionStore.connecting = true;
  connectionStore.error = '';
  try {
    const response = await fetch('/__vault/connect', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ serverAddress, connectionId }),
      signal: AbortSignal.any([controller.signal, AbortSignal.timeout(35000)]),
    });
    const data = await response.json();
    if (!response.ok || !data.connectionId || !data.serverAddress) {
      throw new Error(data.message || 'Could not connect to the Vault server.');
    }
    connectionStore.connection = data;
    sessionStorage.setItem(key, JSON.stringify(data));
    localStorage.setItem('vault:last-server', data.serverAddress);
    configured(data);
    return data;
  } catch (error) {
    connectionStore.error = error.message;
    throw error;
  } finally {
    connectionStore.connecting = false;
  }
}

export async function restoreConnection() {
  try {
    const saved = JSON.parse(sessionStorage.getItem(key) || 'null');
    if (saved) {
      await connectServer(saved.serverAddress, saved.connectionId);
    }
  } catch (error) {
    connectionStore.error = error.message;
  }
}

export function toVaultProxyUrl(value, connection = connectionStore.connection) {
  if (!connection) {
    throw new Error('Connect to a Vault server first.');
  }
  const prefix = `/__vault/${connection.connectionId}`;
  const url = new URL(value, window.location.origin);
  if (url.origin === window.location.origin && url.pathname.startsWith(`${prefix}/AutodeskDM/`)) {
    return `${url.pathname}${url.search}`;
  }
  if (![window.location.origin, connection.serverAddress].includes(url.origin) || !url.pathname.startsWith('/AutodeskDM/')) {
    throw new Error('The resource does not belong to the selected Vault server.');
  }
  return `${prefix}${url.pathname}${url.search}`;
}

export function connectionFetch(url, options = {}) {
  const connection = connectionStore.connection;
  const prefix = connection ? `/__vault/${connection.connectionId}/AutodeskDM/` : null;
  if (!prefix || !String(url).startsWith(prefix)) {
    throw new Error('This request belongs to an inactive server connection.');
  }
  return fetch(url, { ...options, signal: options.signal ? AbortSignal.any([options.signal, controller.signal]) : controller.signal });
}
