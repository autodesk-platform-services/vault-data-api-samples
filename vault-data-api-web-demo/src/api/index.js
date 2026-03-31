import { createAlova } from 'alova';
import GlobalFetch from 'alova/fetch';
import reactHook from 'alova/react';
import { createApis, withConfigType } from './createApis';

export const VAULT_API_BASE_URL = '/AutodeskDM/Services/api/vault/v2'

export const alovaInstance = createAlova({
  baseURL: VAULT_API_BASE_URL,
  statesHook: reactHook,
  requestAdapter: GlobalFetch(),
  beforeRequest: method => {},
  responded: res => {
    return res.json();
  }
});

export const $$userConfigMap = withConfigType({});

/**
 * @type { Apis }
 */
const Apis = createApis(alovaInstance, $$userConfigMap);

export default Apis;
