const PENDING_KEY = 'vault:aps:pending';
const AUTHORIZE_URL = 'https://developer.api.autodesk.com/authentication/v2/authorize';
const TOKEN_URL = 'https://developer.api.autodesk.com/authentication/v2/token';
export const APS_SCOPES = 'user:read data:read data:write data:create openid profapi:core-std-profile:read';

function base64url(bytes) {
  return btoa(String.fromCharCode(...bytes)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

export async function codeChallenge(verifier) {
  return base64url(new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier))));
}

export async function createAuthorizationUrl(clientId, redirectUri, storage = sessionStorage) {
  if (!clientId?.trim()) {
    throw new Error('Enter your APS Client ID.');
  }
  const verifier = base64url(crypto.getRandomValues(new Uint8Array(48)));
  const state = base64url(crypto.getRandomValues(new Uint8Array(32)));
  const url = new URL(AUTHORIZE_URL);
  url.search = new URLSearchParams({
    response_type: 'code', client_id: clientId.trim(), redirect_uri: redirectUri,
    scope: APS_SCOPES, state, code_challenge: await codeChallenge(verifier), code_challenge_method: 'S256',
  });
  storage.setItem(PENDING_KEY, JSON.stringify({ clientId: clientId.trim(), redirectUri, verifier, state, createdAt: Date.now() }));
  return url.href;
}

export async function completeAuthorization(callbackUrl, storage = sessionStorage) {
  const callback = new URL(callbackUrl);
  const pendingText = storage.getItem(PENDING_KEY);
  storage.removeItem(PENDING_KEY);
  let pending;
  try {
    pending = JSON.parse(pendingText || 'null');
  } catch {
    throw new Error('The sign-in state is invalid. Start sign-in again.');
  }
  if (!pending || !Number.isFinite(pending.createdAt) || pending.state !== callback.searchParams.get('state') || Date.now() - pending.createdAt > 10 * 60 * 1000
    || `${callback.origin}${callback.pathname}` !== pending.redirectUri || !pending.verifier) {
    throw new Error('The sign-in state is invalid or expired. Start sign-in again.');
  }
  if (callback.searchParams.has('error')) {
    throw new Error('Autodesk sign-in was canceled or denied. Start sign-in again.');
  }
  const code = callback.searchParams.get('code');
  if (!code) {
    throw new Error('Autodesk did not return an authorization code.');
  }
  const response = await fetch(TOKEN_URL, {
    method: 'POST', signal: AbortSignal.timeout(60000),
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', Accept: 'application/json' },
    body: new URLSearchParams({ grant_type: 'authorization_code', client_id: pending.clientId,
      redirect_uri: pending.redirectUri, code_verifier: pending.verifier, code }),
  });
  if (!response.ok) {
    throw new Error(`Autodesk token exchange failed (HTTP ${response.status}). Check your APS application type and registered callback URL.`);
  }
  const token = await response.json();
  if (!token.access_token || token.token_type?.toLowerCase() !== 'bearer' || !Number.isFinite(Number(token.expires_in)) || !(Number(token.expires_in) > 0)) {
    throw new Error('Autodesk returned an invalid access token response.');
  }
  // Match the WPF demo: sign in again at expiry; do not persist a refresh token.
  return { accessToken: token.access_token, expiresAt: Date.now() + Number(token.expires_in) * 1000, clientId: pending.clientId };
}

export function createAutodeskSession(oauth, user, vault) {
  if (!oauth?.accessToken || oauth.expiresAt <= Date.now()) {
    throw new Error('Your Autodesk ID access token has expired. Sign in again.');
  }
  if (!user?.id || !vault?.id) {
    throw new Error('Vault did not resolve your Autodesk ID to a Vault user. Check the account mapping.');
  }
  return {
    authMethod: 'autodesk', token: `Bearer ${oauth.accessToken}`, userId: user.id,
    userName: user.systemName || user.userName || user.name || 'Autodesk ID',
    vaultId: vault.id, vaultName: vault.name,
  };
}
