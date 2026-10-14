import { useEffect, useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { useSnapshot } from 'valtio';
import { Alert, Spin } from 'antd';
import { getFileVersion, getFileAttachment } from '~/store/filesStore';
import { authStore } from '~/store/auth';
import Viewer from './-explorer/viewer';

export const Route = createFileRoute('/_workspace/explorer/files/$id')({ component: Component });
const isViz = name => /\.dwfx?$/i.test(name || '');
function Component() {
  const { id } = Route.useParams();
  const { session } = useSnapshot(authStore);
  const [urn, setUrn] = useState(null);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let active = true;
    setLoading(true);
    setUrn(null);
    setError(null);
    (async () => {
      try {
        const file = await getFileVersion(id);
        if (!active) return;
        const visualization = isViz(file.name) ? file : file.hasVisualizationAttachment ? await getFileAttachment(id) : null;
        if (!visualization?.url) {
          throw new Error('Unable to view this file. Generate a DWF or DWFx visualization in Vault first.');
        }
        if (active) setUrn(`${visualization.url}/svf/bubble.json`);
      } catch (failure) {
        if (active) setError(failure.message);
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [id]);
  if (loading) return <Spin tip="Loading file details..."><div style={{ height: 100 }} /></Spin>;
  if (error) return <Alert type="error" showIcon message={error} />;
  return urn ? <Viewer urn={urn} session={session} /> : null;
}
