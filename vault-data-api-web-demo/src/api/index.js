import { createAlova } from 'alova';
import GlobalFetch from 'alova/fetch';
import reactHook from 'alova/react';
import { createApis, withConfigType } from './createApis';

export const alovaInstance = createAlova({
  baseURL: '/AutodeskDM/Services/api/vault/v1',
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
