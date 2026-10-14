import { Outlet, Link, useNavigate, useLocation, useParams } from '@tanstack/react-router';
import { Alert, Button, Spin, Tabs, Tree } from 'antd';
import { useEffect, useState } from 'react';
import { useSnapshot } from 'valtio';
import { filesStore, initializeExplorer, loadFolder, explorer } from '~/features/explorer/filesStore';
import { resolveFolderId } from '~/features/explorer/folderTree';
import Split from 'react-split';

export default function ExplorerPage({ basePath }) {
  const navigate = useNavigate();
  const { rootFolder, rootLoading, rootError, directoryStates } = useSnapshot(filesStore);
  const { tree } = useSnapshot(explorer);
  const { id } = useParams({ strict: false });
  const [expandedKeys, setExpandedKeys] = useState([]);
  const [treeError, setTreeError] = useState(null);
  const selectedFolderId = resolveFolderId(id, rootFolder);
  const current = directoryStates[String(selectedFolderId)];
  const { pathname } = useLocation();
  const fromPath = id ? `${basePath}/$id` : basePath;
  const lastSegment = pathname.split('/').pop();
  const selectedTab = ['folders', 'files', 'links'].includes(lastSegment) ? lastSegment : 'folders';

  useEffect(() => { initializeExplorer().catch(() => {}); }, []);
  useEffect(() => {
    if (rootFolder?.id) setExpandedKeys([String(rootFolder.id)]);
  }, [rootFolder?.id]);
  useEffect(() => {
    if (selectedFolderId) loadFolder(selectedFolderId).catch(() => {});
  }, [selectedFolderId]);

  const error = rootError || current?.error || treeError?.message;
  const retry = () => {
    if (rootError) {
      initializeExplorer().catch(() => {});
    } else {
      const retryId = current?.error ? selectedFolderId : treeError?.id;
      setTreeError(null);
      loadFolder(retryId, { refresh: true }).catch(failure => setTreeError({ id: retryId, message: failure.message }));
    }
  };
  return <Split className="split" sizes={[25, 75]} expandToMin minSize={100} gutterSize={10}
    snapOffset={30} dragInterval={1} direction="horizontal" cursor="col-resize" gutterAlign="start">
    <div style={{ backgroundColor: '#fff', height: '100%', overflow: 'auto', padding: 8 }}>
      {error && <Alert type="error" message={error} action={<Button size="small" onClick={retry}>Retry</Button>} />}
      {rootLoading && <Spin tip="Loading root folder..."><div style={{ height: 60 }} /></Spin>}
      <Tree.DirectoryTree treeData={tree} expandedKeys={expandedKeys} onExpand={setExpandedKeys}
        loadedKeys={Object.keys(directoryStates).filter(key => directoryStates[key].loaded)}
        loadData={node => loadFolder(node.id).catch(failure => {
          setTreeError({ id: node.id, message: failure.message });
          throw failure;
        })}
        selectedKeys={selectedFolderId ? [String(selectedFolderId)] : []}
        onSelect={(keys, info) => navigate({ to: `/explorer/$id/${selectedTab}`, params: { id: info.node.id } })} />
    </div>
    <div style={{ paddingLeft: 8 }}>
      {current?.loading && <div role="status" style={{ padding: 8 }}><Spin size="small" /> Loading folder… {current.received || 0} entries received</div>}
      <Tabs activeKey={selectedTab} items={['folders', 'files', 'links'].map(tab => ({
        key: tab, label: <Link from={fromPath} to={`./${tab}`}>{tab[0].toUpperCase() + tab.slice(1)}</Link>,
        children: <Outlet />,
      }))} />
    </div>
  </Split>;
}
