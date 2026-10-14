export function createFolderLoader({ fetchRoot, fetchFolder, fetchContents, onChange = () => {} }) {
  const empty = () => ({ rootFolder: null, rootLoading: false, rootError: null, knownFolders: {}, directories: {}, dataVersion: 0 });
  let state = empty();
  let generation = 0;
  let rootPending;
  const pending = new Map();
  const emit = () => onChange(state);

  async function initialize() {
    if (state.rootFolder) return state.rootFolder;
    if (rootPending) return rootPending;
    const version = generation;
    state.rootLoading = true;
    state.rootError = null;
    emit();
    rootPending = Promise.resolve().then(async () => {
      try {
        if (version !== generation) return null;
        const root = await fetchRoot();
        if (version !== generation) return null;
        if (!root?.id) throw new Error('Vault did not return a valid root folder.');
        state.rootFolder = root;
        state.knownFolders[String(root.id)] = root;
        state.dataVersion++;
        return root;
      } catch (error) {
        if (version === generation) state.rootError = error.message;
        throw error;
      } finally {
        if (version === generation) {
          state.rootLoading = false;
          rootPending = null;
          emit();
        }
      }
    });
    return rootPending;
  }

  function removeBranch(id) {
    for (const folder of Object.values(state.knownFolders)) {
      if (String(folder.parentFolderId) === id && String(folder.id) !== id) removeBranch(String(folder.id));
    }
    delete state.knownFolders[id];
    delete state.directories[id];
    pending.delete(id);
  }

  async function load(id = 'root', { refresh = false } = {}) {
    const version = generation;
    const root = await initialize();
    if (version !== generation || !root) return null;
    const key = String(id === 'root' || id == null ? root.id : id);
    if (pending.has(key)) return pending.get(key);
    const previous = state.directories[key];
    if (previous?.loaded && !refresh) return previous;
    const entry = { ...previous, loading: true, error: null, received: 0, loaded: !!previous?.loaded };
    state.directories[key] = entry;
    emit();
    const active = () => version === generation && state.directories[key] === entry;
    const task = Promise.resolve().then(async () => {
      try {
        if (!active()) return null;
        if (!state.knownFolders[key]) {
          const folder = await fetchFolder(key);
          if (!active()) return null;
          if (!folder?.id) throw new Error('Vault did not return the selected folder.');
          state.knownFolders[key] = folder;
          state.dataVersion++;
          emit();
        }
        const response = await fetchContents(key, count => {
          if (active()) {
            entry.received = count;
            emit();
          }
        });
        if (!active()) return null;
        if (!Array.isArray(response?.results)) throw new Error('Vault did not return valid folder contents.');
        const folders = response.results.filter(item => item.entityType === 'Folder');
        const children = new Set(folders.map(folder => String(folder.id)));
        for (const old of previous?.folders || []) {
          if (!children.has(String(old.id))) removeBranch(String(old.id));
        }
        for (const folder of folders) state.knownFolders[String(folder.id)] = folder;
        Object.assign(entry, {
          folders,
          files: response.results.filter(item => item.entityType === 'FileVersion'),
          links: response.results.filter(item => item.entityType === 'Link'),
          loaded: true, received: response.results.length, version: (previous?.version || 0) + 1,
        });
        state.dataVersion++;
        return entry;
      } catch (error) {
        if (active()) entry.error = error.message;
        throw error;
      } finally {
        if (active()) {
          entry.loading = false;
          pending.delete(key);
          emit();
        }
      }
    });
    pending.set(key, task);
    return task;
  }

  return {
    initialize, load, getState: () => state,
    reset() {
      generation++;
      rootPending = null;
      pending.clear();
      state = empty();
      emit();
    },
  };
}
