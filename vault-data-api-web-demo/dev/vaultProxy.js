import http from 'node:http';
import https from 'node:https';
import { randomUUID } from 'node:crypto';

export function normalizeServerAddress(value) {
  let url;
  try {
    url = new URL(String(value || '').trim());
  } catch {
    throw new Error('Enter the server root address starting with http:// or https://.');
  }
  if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.pathname !== '/' || url.search || url.hash) {
    throw new Error('Enter an HTTP or HTTPS server root address, without credentials, API path, query, or fragment.');
  }
  return url.origin;
}

function inspectServer(origin, signal, redirects = 0) {
  return new Promise((resolve, reject) => {
    const url = new URL('/AutodeskDM/Services/api/vault/v2/server-info', origin);
    const request = (url.protocol === 'https:' ? https : http).get(url, { signal }, response => {
      if ([301, 302, 307, 308].includes(response.statusCode) && response.headers.location) {
        response.resume();
        const next = new URL(response.headers.location, url);
        if (redirects >= 3 || next.hostname !== url.hostname || next.protocol !== 'https:' || next.pathname !== url.pathname) {
          reject(new Error('The API redirected to a different server or login page. Enter the final Vault server address.'));
          return;
        }
        inspectServer(next.origin, signal, redirects + 1).then(resolve, reject);
        return;
      }
      let body = '';
      response.setEncoding('utf8');
      response.on('data', chunk => {
        body += chunk;
        if (body.length > 1024 * 1024) {
          response.destroy(new Error('Server info response is too large.'));
        }
      });
      response.on('error', reject);
      response.on('end', () => {
        try {
          if (response.statusCode !== 200) {
            throw new Error(`Cannot connect to Vault server (HTTP ${response.statusCode}).`);
          }
          resolve({ serverAddress: origin, serverInfo: JSON.parse(body) });
        } catch (error) {
          reject(error);
        }
      });
    });
    request.on('error', reject);
  });
}

export function createVaultProxyMiddleware() {
  const connections = new Map();
  const json = (res, status, data) => {
    res.writeHead(status, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
    res.end(JSON.stringify(data));
  };
  return async (req, res, next) => {
    const authority = req.headers[':authority'] || req.headers.host;
    if (req.url === '/__vault/connect' && req.method === 'POST') {
      try {
        if (req.headers.origin && new URL(req.headers.origin).host !== authority) {
          json(res, 403, { message: 'Connect must be called from the local demo.' });
          return;
        }
        let body = '';
        for await (const chunk of req) {
          body += chunk;
          if (body.length > 4096) {
            throw new Error('Connection configuration is too large.');
          }
        }
        const input = JSON.parse(body);
        const target = normalizeServerAddress(input.serverAddress);
        if (new URL(target).host === authority) {
          throw new Error('Enter the Vault server address, not the demo address.');
        }
        const id = input.connectionId || randomUUID();
        if (!/^[a-f0-9-]{36}$/i.test(id)) {
          throw new Error('Invalid connection identifier.');
        }
        const existing = connections.get(id);
        if (existing && existing.serverAddress !== target) {
          throw new Error('Use a new connection when changing servers.');
        }
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), 30000);
        const cancel = () => { if (!res.writableEnded) controller.abort(); };
        res.once('close', cancel);
        let details;
        try {
          details = await inspectServer(target, controller.signal);
        } finally {
          clearTimeout(timer);
          res.removeListener('close', cancel);
        }
        connections.set(id, details);
        json(res, 200, { connectionId: id, ...details });
      } catch (error) {
        if (!res.destroyed) {
          json(res, 400, { message: error.message });
        }
      }
      return;
    }
    const match = req.url?.match(/^\/__vault\/([a-f0-9-]{36})(\/AutodeskDM(?:\/|\?|$).*)/i);
    if (!match) {
      next();
      return;
    }
    const connection = connections.get(match[1]);
    if (!connection) {
      json(res, 404, { message: 'Connection expired. Connect to the Vault server again.' });
      return;
    }
    const target = new URL(match[2], connection.serverAddress);
    const headers = { ...req.headers, host: target.host };
    for (const name of Object.keys(headers)) {
      if (name.startsWith(':') || ['connection', 'keep-alive', 'proxy-connection', 'transfer-encoding', 'upgrade'].includes(name)) {
        delete headers[name];
      }
    }
    const upstream = (target.protocol === 'https:' ? https : http).request(target, { method: req.method, headers }, response => {
      const responseHeaders = { ...response.headers, 'cache-control': 'no-store' };
      if (responseHeaders.location) {
        const location = new URL(responseHeaders.location, target);
        const isRedirect = [301, 302, 303, 307, 308].includes(response.statusCode);
        if (isRedirect) {
          if (location.hostname !== target.hostname || !['http:', 'https:'].includes(location.protocol)
            || !location.pathname.startsWith('/AutodeskDM/')) {
            response.resume();
            json(res, 502, { message: `Vault returned HTTP ${response.statusCode} redirect outside its API (${location.origin}${location.pathname}). Check the selected server address and authentication.` });
            return;
          }
          if (location.protocol === 'https:') {
            connection.serverAddress = location.origin;
          }
        }
        // A 201 Location identifies the new resource; it is not a redirect.
        // Gateways may report an internal host, so map its API path to this connection.
        if (['http:', 'https:'].includes(location.protocol) && location.pathname.startsWith('/AutodeskDM/')) {
          responseHeaders.location = `/__vault/${match[1]}${location.pathname}${location.search}`;
        }
      }
      for (const name of ['connection', 'keep-alive', 'proxy-connection', 'transfer-encoding', 'upgrade']) {
        delete responseHeaders[name];
      }
      res.writeHead(response.statusCode, responseHeaders);
      response.on('error', () => res.destroy());
      response.pipe(res);
    });
    upstream.setTimeout(60000, () => upstream.destroy(new Error('Vault proxy request timed out.')));
    upstream.on('error', error => {
      if (!res.headersSent && !res.destroyed) {
        json(res, 502, { message: error.message });
      } else {
        res.destroy();
      }
    });
    req.on('aborted', () => upstream.destroy());
    res.on('close', () => { if (!res.writableEnded) upstream.destroy(); });
    req.pipe(upstream);
  };
}

export default function vaultProxy() {
  return { name: 'local-vault-proxy', configureServer(server) {
    server.middlewares.use(createVaultProxyMiddleware());
  } };
}
