import { proxy } from 'valtio';
import { derive } from 'derive-valtio';
import { authStore } from '~/features/authentication/authStore';
import Apis from '~/api';
import { buildFolderTree } from '~/features/explorer/folderTree';
import { createFolderLoader } from '~/features/explorer/folderLoader';
import { fetchFolderContents } from './explorerApi';
import { folderTransformer1, fileTransformer1, linkTransformer1 } from './transformers';

export const filesStore = proxy({
  loading: false, doneSync: false, rootFolder: null, rootLoading: false, rootError: null,
  directoryStates: {},
  files: fileTransformer1([]), folders: folderTransformer1([]), links: linkTransformer1([]),
});

function folderRequest(id) {
  const session = authStore.session;
  if (!session) throw new Error('Sign in before loading Vault contents.');
  return Apis.vault.getFolderById({
    pathParams: { vaultId: session.vaultId, id }, headers: { authorization: session.token }, cacheFor: 0,
  });
}
let lastState;
let lastDataVersion = -1;
const loader = createFolderLoader({
  fetchRoot: () => folderRequest('root'),
  fetchFolder: folderRequest,
  fetchContents: (id, progress) => fetchFolderContents({ ...authStore.session }, id, progress),
  onChange: state => {
    const entries = Object.entries(state.directories);
    Object.assign(filesStore, {
      rootFolder: state.rootFolder, doneSync: !!state.rootFolder,
      rootLoading: state.rootLoading, rootError: state.rootError,
      loading: state.rootLoading || entries.some(([, entry]) => entry.loading),
      directoryStates: Object.fromEntries(entries.map(([id, entry]) => [id, {
        loaded: entry.loaded, loading: entry.loading, error: entry.error, received: entry.received, version: entry.version,
      }])),
    });
    if (lastState !== state || lastDataVersion !== state.dataVersion) {
      Object.assign(filesStore, {
        folders: folderTransformer1(Object.values(state.knownFolders)),
        files: fileTransformer1(entries.flatMap(([, entry]) => entry.files || [])),
        links: linkTransformer1(entries.flatMap(([, entry]) => entry.links || [])),
      });
      lastState = state;
      lastDataVersion = state.dataVersion;
    }
  },
});

export const initializeExplorer = () => loader.initialize();
export const loadFolder = (id, options) => loader.load(id, options);
export const sync = id => loader.load(id, { refresh: true });
export const resetExplorer = () => loader.reset();

export async function getFileVersion(id) {
  const cached = filesStore.files.objects().find(file => String(file.id) === String(id));
  if (cached) return cached;
  const session = authStore.session;
  const version = await Apis.vault.getFileVersionById({
    pathParams: { vaultId: session.vaultId, id },
    params: { 'option[extendedModels]': true }, headers: { authorization: session.token }, cacheFor: 0,
  });
  return fileTransformer1([version]).objects()[0];
}

export const getFileAttachment = id => Apis.vault.getFileVersionVisualizationAttachment({
  pathParams: { vaultId: authStore.session.vaultId, id }, headers: { authorization: authStore.session.token },
});
export const translateModel = id => Apis.vault.getFileVersionLmvRoot({
  pathParams: { vaultId: authStore.session.vaultId, id }, headers: { authorization: authStore.session.token },
});

export const explorer = derive({ tree: get => {
  const { rootFolder, folders, directoryStates } = get(filesStore);
  return buildFolderTree(rootFolder, folders.objects(), Object.keys(directoryStates).filter(id => directoryStates[id].loaded));
} });
