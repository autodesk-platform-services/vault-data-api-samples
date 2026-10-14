import { proxy } from 'valtio';
import { fetchCurrentUserInfo, loginUser } from './helper';
import { VAULT_API_BASE_URL } from '~/api';
import { completeAuthorization, createAuthorizationUrl, createAutodeskSession } from '~/features/authentication/apsOAuth';
import { connectionStore, connectionFetch, disconnectServer } from '~/features/connection/connection';

const sessionKey = 'vault:session';
const oauthKey = 'vault:aps:session';
let expiryTimer;
let generation = 0;
let resetSessionData = () => {};
export function setSessionReset(callback) {
  resetSessionData = callback;
}
export const AuthStatus = { PENDING: 1, YES: 2, NO: 3 };
export const authStore = proxy({ authStatus: AuthStatus.PENDING, session: null, user: null, oauth: null, error: '' });

function persist() {
  localStorage.removeItem(sessionKey);
  if (authStore.oauth) {
    localStorage.removeItem(sessionKey);
    sessionStorage.setItem(oauthKey, JSON.stringify({ oauth: authStore.oauth, session: authStore.session,
      serverAddress: connectionStore.connection?.serverAddress }));
  } else {
    sessionStorage.removeItem(oauthKey);
    if (authStore.session) {
      sessionStorage.setItem(sessionKey, JSON.stringify(authStore.session));
    } else {
      localStorage.removeItem(sessionKey);
      sessionStorage.removeItem(sessionKey);
    }
  }
}

function armExpiry() {
  clearTimeout(expiryTimer);
  if (authStore.oauth) {
    expiryTimer = setTimeout(() => {
      logout();
      authStore.error = 'Your Autodesk ID access token has expired. Sign in again.';
    }, Math.max(0, authStore.oauth.expiresAt - Date.now()));
  }
}

async function vaultRequest(path, token) {
  const response = await connectionFetch(`${VAULT_API_BASE_URL}${path}`, {
    headers: { Authorization: token }, signal: AbortSignal.timeout(60000), cache: 'no-store',
  });
  if (!response.ok) {
    throw new Error(`Vault authentication failed (HTTP ${response.status}). Check Autodesk ID account mapping and permissions on this server.`);
  }
  return response.json();
}

export function logout() {
  generation++;
  clearTimeout(expiryTimer);
  Object.assign(authStore, { session: null, user: null, oauth: null, authStatus: AuthStatus.NO, error: '' });
  resetSessionData();
  persist();
}
export function changeServer() {
  logout();
  disconnectServer();
}

export async function login(data) {
  const version = ++generation;
  const session = await loginUser(data);
  if (version !== generation) {
    return;
  }
  if (!session) {
    throw new Error('Vault sign-in failed. Check your credentials and selected Vault.');
  }
  const context = {
    serverAddress: connectionStore.connection.serverAddress,
    connectionId: connectionStore.connection.connectionId,
    authMethod: 'vault', token: `Bearer ${session.accessToken}`,
    userId: session.userInformation.id,
    userName: session.userInformation.systemName || session.userInformation.userName,
    vaultId: session.vaultInformation.id || session.vaultInformation.vaultId,
    vaultName: session.vaultInformation.name || session.vaultInformation.vaultName,
  };
  const user = await fetchCurrentUserInfo(context);
  if (version !== generation) {
    return;
  }
  if (!user) {
    throw new Error('Vault could not validate the signed-in user.');
  }
  clearTimeout(expiryTimer);
  Object.assign(authStore, { oauth: null, session: context, user, authStatus: AuthStatus.YES, error: '' });
  persist();
}

export async function signInAutodesk(clientId) {
  if (!connectionStore.connection) {
    throw new Error('Connect to a Vault server first.');
  }
  sessionStorage.setItem('vault:aps:server', connectionStore.connection.serverAddress);
  const url = await createAuthorizationUrl(clientId, `${window.location.origin}/`);
  localStorage.setItem('vault:aps:clientId', clientId.trim());
  logout();
  window.location.assign(url);
}

export async function listVaults() {
  const token = authStore.oauth ? `Bearer ${authStore.oauth.accessToken}` : null;
  return token ? vaultRequest('/vaults', token) : connectionFetch(`${VAULT_API_BASE_URL}/vaults`, { signal: AbortSignal.timeout(60000) }).then(async response => {
    if (!response.ok) {
      throw new Error(`Cannot load Vaults (HTTP ${response.status}).`);
    }
    return response.json();
  });
}

export async function selectAutodeskVault(vault) {
  const version = ++generation;
  const oauth = authStore.oauth;
  if (!vault?.id) {
    throw new Error('Select an available Vault.');
  }
  if (!oauth || oauth.expiresAt <= Date.now()) {
    logout();
    throw new Error('Sign in with Autodesk ID again.');
  }
  const token = `Bearer ${oauth.accessToken}`;
  const resolved = await vaultRequest('/sessions/@current', token);
  if (version !== generation) {
    return;
  }
  const user = resolved.userInformation;
  Object.assign(authStore, { session: { ...createAutodeskSession(oauth, user, vault),
    serverAddress: connectionStore.connection.serverAddress, connectionId: connectionStore.connection.connectionId }, user, authStatus: AuthStatus.YES, error: '' });
  persist();
}

export async function initializeAuth() {
  const version = ++generation;
  try {
    const callback = new URL(window.location.href);
    if (!connectionStore.connection) {
      if (callback.searchParams.has('code') || callback.searchParams.has('error')) {
        window.history.replaceState(null, '', '/');
        sessionStorage.removeItem('vault:aps:pending');
      }
      logout();
      return;
    }
    if (callback.searchParams.has('code') || callback.searchParams.has('error')) {
      const callbackUrl = callback.href;
      window.history.replaceState(null, '', '/');
      const expectedServer = sessionStorage.getItem('vault:aps:server');
      sessionStorage.removeItem('vault:aps:server');
      if (expectedServer !== connectionStore.connection.serverAddress) {
        sessionStorage.removeItem('vault:aps:pending');
        throw new Error('The server changed during Autodesk sign-in. Start sign-in again.');
      }
      const oauth = await completeAuthorization(callbackUrl);
      if (version !== generation) {
        return;
      }
      localStorage.removeItem(sessionKey);
      Object.assign(authStore, { oauth, session: null, user: null });
      persist();
      armExpiry();
    } else {
      const saved = JSON.parse(sessionStorage.getItem(oauthKey) || 'null');
      if (saved?.oauth?.expiresAt > Date.now() && saved.serverAddress === connectionStore.connection.serverAddress) {
        Object.assign(authStore, { oauth: saved.oauth, session: saved.session });
        if (saved.session) {
          const resolved = await vaultRequest('/sessions/@current', `Bearer ${saved.oauth.accessToken}`);
          if (version !== generation) {
            return;
          }
          if (!resolved.userInformation?.id) {
            throw new Error('Vault could not resolve your Autodesk ID account. Sign in again.');
          }
          authStore.user = resolved.userInformation;
          authStore.session = createAutodeskSession(saved.oauth, resolved.userInformation, {
            id: saved.session.vaultId, name: saved.session.vaultName,
          });
          authStore.session.serverAddress = connectionStore.connection.serverAddress;
          authStore.session.connectionId = connectionStore.connection.connectionId;
          persist();
        }
        if (saved.oauth.expiresAt <= Date.now()) {
          throw new Error('Your Autodesk ID access token has expired. Sign in again.');
        }
        armExpiry();
      } else {
        sessionStorage.removeItem(oauthKey);
        localStorage.removeItem(sessionKey);
        const session = JSON.parse(sessionStorage.getItem(sessionKey) || 'null');
        if (session?.serverAddress === connectionStore.connection.serverAddress && session?.connectionId === connectionStore.connection.connectionId) {
          const user = await fetchCurrentUserInfo(session);
          if (version !== generation) {
            return;
          }
          if (!user) {
            throw new Error('Your Vault session is no longer valid. Sign in again.');
          }
          Object.assign(authStore, { session, user });
        } else {
          sessionStorage.removeItem(sessionKey);
        }
      }
    }
    authStore.authStatus = authStore.session ? AuthStatus.YES : AuthStatus.NO;
  } catch (error) {
    logout();
    authStore.error = error.message;
  }
}
