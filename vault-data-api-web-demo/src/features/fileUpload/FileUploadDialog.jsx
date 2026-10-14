/* eslint-disable react/prop-types */
import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Checkbox, Input, Modal, Progress, Space, Typography } from 'antd';
import { VAULT_API_BASE_URL } from '~/api';
import { createFileUploadService } from './fileUploadService';
import { connectionFetch } from '~/features/connection/connection';

export default function FileUploadDialog({ target, session, onClose, onChanged }) {
  const [file, setFile] = useState(null);
  const [comment, setComment] = useState('');
  const [checkoutFirst, setCheckoutFirst] = useState(!target.file?.isCheckedOut);
  const [keepCheckedOut, setKeepCheckedOut] = useState(false);
  const [busy, setBusy] = useState(false);
  const [progress, setProgress] = useState({ stage: 'Select a local file', transferred: 0 });
  const [checkedOut, setCheckedOut] = useState(!!target.file?.isCheckedOut);
  const [error, setError] = useState('');
  const [unknown, setUnknown] = useState(false);
  const [result, setResult] = useState('');
  const controller = useRef(null);
  const running = useRef(false);
  const mounted = useRef(true);
  const isUpdate = target.mode === 'update';

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      controller.current?.abort();
    };
  }, []);

  async function run(undo = false) {
    if (running.current) {
      return;
    }
    running.current = true;
    controller.current = new AbortController();
    setBusy(true);
    setError('');
    setProgress({ stage: undo ? 'Undoing checkout' : 'Starting', transferred: 0 });
    try {
      const service = createFileUploadService({
        baseURL: VAULT_API_BASE_URL, session, signal: controller.current.signal, fetcher: connectionFetch,
        onProgress: (next) => {
          if (mounted.current) {
            setProgress((previous) => ({ ...previous, ...next }));
            if (next.checkedOut) {
              setCheckedOut(true);
              setCheckoutFirst(false);
              onChanged();
            }
          }
        },
      });
      const response = undo
        ? await service.undoCheckout(target.file.masterId)
        : isUpdate
          ? await service.update({ file, masterId: target.file.masterId, comment, checkoutFirst, keepCheckedOut })
          : await service.add({ file, folderId: target.folder.id, comment });
      if (!mounted.current) {
        return;
      }
      const retained = !undo && isUpdate && !!response.data?.isCheckedOut;
      setCheckedOut(retained);
      setResult(undo ? 'Checkout released.' : !isUpdate ? `Added ${file.name}.`
        : response.status === 201 ? `Created version ${response.data?.version ?? ''} of ${target.file.name}.${retained ? ' File remains checked out.' : ''}`
          : `No new version was created. ${retained ? 'File remains checked out.' : 'Checkout released.'}`);
      setProgress((previous) => ({ ...previous, stage: 'Completed' }));
      onChanged();
    } catch (error) {
      if (!mounted.current) {
        return;
      }
      setError(error.message);
      setUnknown(!!error.outcomeUnknown);
      if (error.outcomeUnknown) {
        onChanged();
      }
    } finally {
      running.current = false;
      if (mounted.current) {
        setBusy(false);
      }
    }
  }

  const close = () => {
    if (!running.current) {
      onClose();
    }
  };
  const percent = progress.total > 0 ? Math.floor(100 * (progress.transferred || 0) / progress.total) : 0;
  return <Modal title={isUpdate ? 'Update File (Check In)' : 'Add File'} open onCancel={close}
    maskClosable={!busy} keyboard={!busy} closable={!busy} footer={null}>
    <Space direction="vertical" style={{ width: '100%' }} size="middle">
      <Typography.Text>{isUpdate ? `Vault file: ${target.file.name} (version ${target.file.version})`
        : `Destination: ${target.folder.fullName || target.folder.name}`}</Typography.Text>
      <label>Local file
        <input type="file" disabled={busy || !!result || unknown} style={{ display: 'block', marginTop: 8 }}
          onChange={(event) => { setFile(event.target.files?.[0] || null); setError(''); }} />
      </label>
      {isUpdate && file && file.name !== target.file.name && <Alert type="info" showIcon
        message={`The selected content will replace ${target.file.name}; the Vault file will keep its name.`} />}
      <Input.TextArea aria-label="Comment" placeholder="Comment (optional)" value={comment}
        disabled={busy || !!result || unknown} onChange={(event) => setComment(event.target.value)} />
      {isUpdate && <>
        <Checkbox checked={checkoutFirst} disabled={busy || checkedOut || !!result || unknown}
          onChange={(event) => setCheckoutFirst(event.target.checked)}>Check out before uploading</Checkbox>
        <Checkbox checked={keepCheckedOut} disabled={busy || !!result || unknown}
          onChange={(event) => setKeepCheckedOut(event.target.checked)}>Keep checked out after check-in</Checkbox>
        <Typography.Text type="secondary">If already checked out, check-in requires the checkout to belong to your current user.</Typography.Text>
      </>}
      <Typography.Text role="status">{progress.stage}</Typography.Text>
      {progress.total !== undefined && <Progress percent={percent} status={error ? 'exception' : busy ? 'active' : 'normal'} />}
      {result && <Alert type="success" showIcon message={result} />}
      {error && <Alert type="error" showIcon message={error} />}
      {unknown && <Alert type="warning" showIcon message="Vault may have applied the last request. Verify the file and checkout status before retrying; this operation will not be submitted again from this dialog." />}
      {checkedOut && !unknown && <Alert type="warning" showIcon message="The file remains checked out. Canceling an upload does not release its checkout." />}
      {(result || unknown) && <Typography.Text type="secondary">Close this dialog and use Refresh in the Files toolbar to load the current Vault state.</Typography.Text>}
      <Space wrap>
        {busy ? <Button onClick={() => controller.current?.abort()}>Cancel operation</Button> : <>
          {!result && !unknown && <Button type="primary" disabled={!file} onClick={() => run()}>{isUpdate ? 'Upload and Check In' : 'Upload and Add'}</Button>}
          {isUpdate && checkedOut && !unknown && <Button onClick={() => Modal.confirm({
            title: 'Undo Checkout?', content: 'Release your checkout and keep the current committed file content.',
            onOk: () => run(true),
          })}>Undo Checkout</Button>}
          <Button onClick={close}>Close</Button>
        </>}
      </Space>
    </Space>
  </Modal>;
}
