const PREFERRED_PART_SIZE = 8 * 1024 * 1024;

export class FileOperationError extends Error {
  constructor(message, outcomeUnknown = false) {
    super(message);
    this.name = 'FileOperationError';
    this.outcomeUnknown = outcomeUnknown;
  }
}

export function createFileUploadService({ baseURL, session, signal, onProgress = () => {}, fetcher = fetch }) {
  if (!session?.vaultId || !session?.token) {
    throw new Error('No active Vault session.');
  }
  const root = `${baseURL}/vaults/${encodeURIComponent(session.vaultId)}`;
  const report = (stage, values = {}) => onProgress({ stage, ...values });

  async function request(path, { method = 'POST', data, token, binary = false, commit = false } = {}) {
    signal?.throwIfAborted();
    const controller = new AbortController();
    const abort = () => controller.abort(signal.reason);
    signal?.addEventListener('abort', abort, { once: true });
    const timeout = setTimeout(() => controller.abort(new Error('Vault request timed out after one minute.')), 60000);
    let response;
    try {
      response = await fetcher(`${root}${path}`, {
        method,
        signal: controller.signal,
        headers: {
          Authorization: session.token,
          ...(data !== undefined ? { 'Content-Type': binary ? 'application/octet-stream' : 'application/json' } : {}),
          ...(token ? { 'X-Vault-Upload-Session': token } : {}),
        },
        body: data === undefined ? undefined : binary ? data : JSON.stringify(data),
      });
      const text = await response.text();
      let body;
      try {
        body = text ? JSON.parse(text) : null;
      } catch {
        throw new FileOperationError(`Vault returned an unreadable response (HTTP ${response.status}).`, commit && (response.ok || response.status >= 500));
      }
      if (!response.ok) {
        const detail = body?.message || body?.detail || body?.title || body?.error?.message;
        throw new FileOperationError(detail || `Vault request failed (HTTP ${response.status}).`, commit && response.status >= 500);
      }
      if (!body || typeof body !== 'object' || Array.isArray(body)) {
        throw new FileOperationError('Vault returned an invalid response. Verify the file status before retrying.', commit);
      }
      return { status: response.status, data: body };
    } catch (error) {
      if (error instanceof FileOperationError) {
        throw error;
      }
      throw new FileOperationError(
        controller.signal.aborted ? controller.signal.reason?.message || 'The operation was canceled.' : error.message,
        commit,
      );
    } finally {
      clearTimeout(timeout);
      signal?.removeEventListener('abort', abort);
    }
  }

  async function upload(file) {
    report('Creating upload session');
    const { data: session } = await request('/file-uploads', { data: { fileName: file.name } });
    if (!session?.uploadId || !session.uploadSessionToken || !Number.isSafeInteger(session.maxPartSize) || session.maxPartSize <= 0) {
      throw new Error('Vault returned an invalid upload session.');
    }
    const size = Math.min(PREFERRED_PART_SIZE, session.maxPartSize);
    const uploadPath = `/file-uploads/${encodeURIComponent(session.uploadId)}`;
    let token = session.uploadSessionToken;
    let transferred = 0;
    // Empty files still require a zero-byte part, as in the WPF sample.
    for (let partIndex = 0; partIndex === 0 || transferred < file.size; partIndex++) {
      const end = Math.min(transferred + size, file.size);
      report(`Uploading part ${partIndex + 1}`, { transferred, total: file.size });
      const { data: part } = await request(`${uploadPath}/parts/${partIndex}`, {
        method: 'PUT', data: file.slice(transferred, end), token, binary: true,
      });
      if (!part?.uploadSessionToken) {
        throw new Error('Vault did not return the next upload token. Start a new upload.');
      }
      token = part.uploadSessionToken;
      transferred = end;
      report(`Uploaded part ${partIndex + 1}`, { transferred, total: file.size });
    }
    report('Completing upload', { transferred, total: file.size });
    const { data: completed } = await request(`${uploadPath}:complete`, { token });
    if (!completed?.uploadTicket) {
      throw new Error('Vault did not return an upload ticket.');
    }
    return completed.uploadTicket;
  }

  return {
    async getFolder(id) {
      const { data } = await request(`/folders/${encodeURIComponent(id)}`, { method: 'GET' });
      if (!data?.id) {
        throw new Error('Vault did not return the destination folder.');
      }
      return data;
    },
    async add({ file, folderId, comment }) {
      const numericFolderId = Number(folderId);
      if (!Number.isSafeInteger(numericFolderId) || numericFolderId <= 0) {
        throw new Error('The destination folder has an invalid identifier.');
      }
      const uploadTicket = await upload(file);
      report('Adding file to Vault');
      return request('/files', { commit: true, data: {
        folderId: numericFolderId, name: file.name, uploadTicket,
        ...(comment?.trim() ? { comment: comment.trim() } : {}),
      } });
    },
    async update({ file, masterId, comment, checkoutFirst, keepCheckedOut }) {
      if (!masterId) {
        throw new Error('The selected file has no master ID.');
      }
      const path = `/files/${encodeURIComponent(masterId)}`;
      if (checkoutFirst) {
        report('Checking out file');
        await request(`${path}:checkout`, { commit: true });
        report('File checked out', { checkedOut: true });
      }
      const uploadTicket = await upload(file);
      report('Checking in file');
      return request(`${path}:checkin`, { commit: true, data: {
        uploadTicket, keepCheckedOut,
        ...(comment?.trim() ? { comment: comment.trim() } : {}),
      } });
    },
    async undoCheckout(masterId) {
      if (!masterId) {
        throw new Error('The selected file has no master ID.');
      }
      return request(`/files/${encodeURIComponent(masterId)}:undo-checkout`, { commit: true });
    },
  };
}
