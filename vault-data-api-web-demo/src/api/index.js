import { createAlova } from 'alova';
import GlobalFetch from 'alova/fetch';
import reactHook from 'alova/react';
import { createApis, withConfigType } from './createApis';
import { connectionBase, connectionStore, getConnectionSignal, onConnectionConfigured } from '~/features/connection/connection';
import { invalidateCache } from 'alova';

export let VAULT_API_BASE_URL = connectionBase()
const fetchAdapter = GlobalFetch();

export const alovaInstance = createAlova({
  baseURL: VAULT_API_BASE_URL,
  statesHook: reactHook,
  requestAdapter: (elements, method) => {
    if (!connectionStore.connection || !elements.url.startsWith(`/__vault/${connectionStore.connection.connectionId}/AutodeskDM/`)) {
      throw new Error('This request belongs to an inactive server connection.');
    }
    const signal = getConnectionSignal();
    signal.throwIfAborted();
    const adapter = fetchAdapter(elements, method);
    const abort = () => adapter.abort();
    signal.addEventListener('abort', abort, { once: true });
    return { ...adapter, response: () => adapter.response().finally(() => signal.removeEventListener('abort', abort)) };
  },
  responded: async res => {
    const data = await res.json();
    if (!res.ok) {
      throw new Error(data?.message || data?.detail || `Vault request failed (HTTP ${res.status}).`);
    }
    return data;
  }
});
onConnectionConfigured(connection => {
  VAULT_API_BASE_URL = connectionBase(connection);
  alovaInstance.options.baseURL = VAULT_API_BASE_URL;
  invalidateCache();
});

export const $$userConfigMap = withConfigType({});

/**
 * @type { Apis }
 */
const Apis = createApis(alovaInstance, $$userConfigMap);

export default Apis;
