export function resolveFolderId(id, rootFolder) {
  return id === undefined || id === null || id === 'root' ? rootFolder?.id : id;
}

export function buildFolderTree(rootFolder, folders, loadedIds) {
  if (!rootFolder?.id) {
    return [];
  }
  const rootId = String(rootFolder.id);
  const children = new Map();
  for (const folder of folders) {
    if (String(folder.id) === rootId) {
      continue;
    }
    const parentId = String(folder.parentId);
    if (!children.has(parentId)) {
      children.set(parentId, []);
    }
    children.get(parentId).push(folder);
  }
  const node = (folder) => ({
    key: String(folder.id),
    id: String(folder.id),
    title: folder.name || folder.fullName || 'Vault root',
    children: (children.get(String(folder.id)) || []).map(node),
    ...(loadedIds ? { isLeaf: loadedIds.includes(String(folder.id))
      ? !(children.get(String(folder.id)) || []).length
      : folder.subfolderCount !== undefined && Number(folder.subfolderCount) === 0 } : {}),
  });
  return [node(rootFolder)];
}
