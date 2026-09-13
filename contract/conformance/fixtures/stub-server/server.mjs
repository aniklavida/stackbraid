// A deliberately non-conforming stub of the Identity API, used as a test
// fixture ONLY to prove the conformance suite actually catches violations.
// It is not a real backend, does not implement authorization/permissions,
// stores nothing durably, and must never be mistaken for evidence any real
// backend conforms — see fixtures/stub-server/README.md.
//
// Baseline (STUB_VIOLATIONS unset) tries to be a faithful, minimal
// implementation of contract/openapi.yaml. Setting STUB_VIOLATIONS to a
// comma-separated list of the names below deliberately corrupts one class
// of response at a time, so the demo script can show the suite catching
// each in isolation.

import http from 'node:http';
import crypto from 'node:crypto';

const PORT = Number(process.env.PORT || 4100);
// Short by design: the conformance suite's expiry check waits for a real
// access token to expire rather than skipping, and it will only do that
// within a bounded window (CONFORMANCE_MAX_EXPIRY_WAIT_MS, default 65s).
const ACCESS_TTL_MS = Number(process.env.STUB_ACCESS_TTL_MS || 5000);

const KNOWN_VIOLATIONS = new Set(['wrong-type', 'missing-field', 'bad-timestamp', 'bad-error-shape', 'bad-pagination']);
const VIOLATIONS = new Set(
  (process.env.STUB_VIOLATIONS || '')
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean),
);
for (const v of VIOLATIONS) {
  if (!KNOWN_VIOLATIONS.has(v)) {
    console.error(`Unknown STUB_VIOLATIONS entry: '${v}'. Known: ${[...KNOWN_VIOLATIONS].join(', ')}`);
    process.exit(1);
  }
}

const ROLES = [
  { id: 'aaaaaaaa-0000-4000-8000-000000000001', name: 'admin', description: 'Full access', permissions: ['users:read', 'users:write', 'roles:read', 'roles:write'] },
  { id: 'aaaaaaaa-0000-4000-8000-000000000002', name: 'member', description: 'Standard access', permissions: ['users:read'] },
];

const users = new Map(); // id -> record
const usersByEmail = new Map(); // email -> id
const accessTokens = new Map(); // token -> { userId, expiresAt: epochMs }
const refreshTokens = new Map(); // token -> { userId, revoked }

const newId = () => crypto.randomUUID();
const newToken = () => crypto.randomBytes(24).toString('hex');

function isoTimestamp(date = new Date()) {
  const iso = date.toISOString(); // e.g. 2026-09-13T10:15:30.123Z
  if (VIOLATIONS.has('bad-timestamp')) {
    // The exact divergence a spike caught between .NET and Python: a numeric
    // offset where the contract requires 'Z'.
    return iso.replace('Z', '+00:00');
  }
  return iso;
}

function problem({ status, title, code, detail, errors }) {
  if (VIOLATIONS.has('bad-error-shape')) {
    // Deliberately not RFC 9457: a flat, ad-hoc shape and the wrong content-type.
    return { status, contentType: 'application/json', body: { error: title, message: detail || title } };
  }
  const body = { type: 'about:blank', title, status, code, traceId: newId() };
  if (detail) body.detail = detail;
  if (errors) body.errors = errors;
  return { status, contentType: 'application/problem+json', body };
}

function serializeUser(user) {
  const body = {
    id: user.id,
    email: user.email,
    displayName: user.displayName,
    status: user.status,
    roles: user.roles.map((rid) => ROLES.find((r) => r.id === rid)).filter(Boolean),
    createdAt: user.createdAt,
    updatedAt: user.updatedAt,
    lastLoginAt: user.lastLoginAt,
  };
  if (VIOLATIONS.has('missing-field')) delete body.displayName; // required field silently dropped
  if (VIOLATIONS.has('wrong-type')) body.status = body.status === 'active'; // enum string -> boolean
  return body;
}

function serializeRoles(roles) {
  let list = roles;
  if (VIOLATIONS.has('missing-field')) list = list.map(({ description, ...rest }) => rest); // description isn't required, but permissions/id/name loss below is
  if (VIOLATIONS.has('wrong-type')) list = list.map((r) => ({ ...r, permissions: r.permissions.join(',') })); // array -> string
  return list;
}

function issueTokenPair(userId) {
  const accessToken = newToken();
  const refreshToken = newToken();
  const expiresAtDate = new Date(Date.now() + ACCESS_TTL_MS);
  accessTokens.set(accessToken, { userId, expiresAt: expiresAtDate.getTime() });
  refreshTokens.set(refreshToken, { userId, revoked: false });
  const pair = { accessToken, refreshToken, tokenType: 'Bearer', expiresAt: isoTimestamp(expiresAtDate) };
  if (VIOLATIONS.has('missing-field')) delete pair.tokenType; // keep both tokens intact — that's a different class
  if (VIOLATIONS.has('wrong-type')) pair.expiresAt = Math.floor(expiresAtDate.getTime() / 1000); // string -> number
  return pair;
}

function parseCookies(header) {
  const out = {};
  if (!header) return out;
  for (const part of header.split(';')) {
    const eq = part.indexOf('=');
    if (eq === -1) continue;
    out[part.slice(0, eq).trim()] = part.slice(eq + 1).trim();
  }
  return out;
}

function setRefreshCookie(res, value) {
  res.setHeader('Set-Cookie', `refreshToken=${value}; HttpOnly; Secure; SameSite=Strict; Path=/v1/auth`);
}

function readJsonBody(req) {
  return new Promise((resolve, reject) => {
    let data = '';
    req.on('data', (chunk) => {
      data += chunk;
    });
    req.on('end', () => {
      if (!data) return resolve(undefined);
      try {
        resolve(JSON.parse(data));
      } catch (err) {
        reject(err);
      }
    });
    req.on('error', reject);
  });
}

function getBearerToken(req) {
  const match = /^Bearer\s+(.+)$/i.exec(req.headers['authorization'] || '');
  return match ? match[1] : null;
}

function authenticate(req) {
  const token = getBearerToken(req);
  if (!token) return null;
  const entry = accessTokens.get(token);
  if (!entry || entry.expiresAt < Date.now()) return null;
  return users.get(entry.userId) || null;
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, `http://localhost:${PORT}`);
  const send = (result) => {
    res.statusCode = result.status;
    if (result.body === undefined) {
      res.end();
      return;
    }
    res.setHeader('content-type', `${result.contentType}; charset=utf-8`);
    res.end(JSON.stringify(result.body));
  };
  const sendPair = (pair) => {
    res.statusCode = 200;
    res.setHeader('content-type', 'application/json; charset=utf-8');
    setRefreshCookie(res, pair.refreshToken);
    res.end(JSON.stringify(pair));
  };

  let body;
  try {
    body = await readJsonBody(req);
  } catch {
    return send({ status: 400, contentType: 'application/json', body: { error: 'invalid JSON body' } });
  }

  try {
    if (req.method === 'POST' && url.pathname === '/v1/auth/register') {
      const { email, password, displayName } = body || {};
      const errors = {};
      if (typeof email !== 'string' || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) errors.email = ['must be a valid email address'];
      if (typeof password !== 'string' || password.length < 8) errors.password = ['must be at least 8 characters'];
      if (typeof displayName !== 'string' || displayName.length < 1) errors.displayName = ['must not be empty'];
      if (Object.keys(errors).length > 0) return send(problem({ status: 400, title: 'Validation failed', code: 'IDENTITY.VALIDATION_FAILED', errors }));
      if (usersByEmail.has(email)) return send(problem({ status: 409, title: 'Email already registered', code: 'IDENTITY.EMAIL_TAKEN' }));

      const id = newId();
      const ts = isoTimestamp();
      const user = { id, email, password, displayName, status: 'active', roles: [], createdAt: ts, updatedAt: ts, lastLoginAt: null };
      users.set(id, user);
      usersByEmail.set(email, id);
      return send({ status: 201, contentType: 'application/json', body: serializeUser(user) });
    }

    if (req.method === 'POST' && url.pathname === '/v1/auth/login') {
      const { email, password } = body || {};
      const userId = usersByEmail.get(email);
      const user = userId ? users.get(userId) : null;
      if (!user || user.password !== password) return send(problem({ status: 401, title: 'Invalid credentials', code: 'IDENTITY.INVALID_CREDENTIALS' }));
      user.lastLoginAt = isoTimestamp();
      return sendPair(issueTokenPair(user.id));
    }

    if (req.method === 'POST' && url.pathname === '/v1/auth/refresh') {
      const cookies = parseCookies(req.headers['cookie']);
      const presented = (body && body.refreshToken) || cookies.refreshToken;
      const entry = presented ? refreshTokens.get(presented) : null;
      if (!entry || entry.revoked) return send(problem({ status: 401, title: 'Invalid refresh token', code: 'IDENTITY.INVALID_REFRESH_TOKEN' }));
      entry.revoked = true; // rotation
      return sendPair(issueTokenPair(entry.userId));
    }

    if (req.method === 'POST' && url.pathname === '/v1/auth/logout') {
      const user = authenticate(req);
      if (!user) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      const cookies = parseCookies(req.headers['cookie']);
      const presented = (body && body.refreshToken) || cookies.refreshToken;
      if (presented) {
        const entry = refreshTokens.get(presented);
        if (entry) entry.revoked = true;
      }
      res.statusCode = 204;
      res.end();
      return;
    }

    if (req.method === 'GET' && url.pathname === '/v1/auth/me') {
      const user = authenticate(req);
      if (!user) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      return send({ status: 200, contentType: 'application/json', body: serializeUser(user) });
    }

    if (req.method === 'GET' && url.pathname === '/v1/users') {
      const caller = authenticate(req);
      if (!caller) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      const page = Number(url.searchParams.get('page') || '1');
      const pageSize = Number(url.searchParams.get('pageSize') || '20');
      if (!Number.isInteger(page) || page < 1 || !Number.isInteger(pageSize) || pageSize < 1 || pageSize > 100) {
        return send(problem({ status: 400, title: 'Validation failed', code: 'IDENTITY.VALIDATION_FAILED', errors: { pageSize: ['must be between 1 and 100'] } }));
      }
      let list = [...users.values()];
      const status = url.searchParams.get('status');
      if (status) list = list.filter((u) => u.status === status);
      const roleId = url.searchParams.get('roleId');
      if (roleId) list = list.filter((u) => u.roles.includes(roleId));
      const search = url.searchParams.get('search');
      if (search) {
        const needle = search.toLowerCase();
        list = list.filter((u) => u.email.toLowerCase().includes(needle) || u.displayName.toLowerCase().includes(needle));
      }
      const sort = url.searchParams.get('sort') || '-createdAt';
      const desc = sort.startsWith('-');
      const sortField = desc ? sort.slice(1) : sort;
      list.sort((a, b) => (a[sortField] < b[sortField] ? (desc ? 1 : -1) : a[sortField] > b[sortField] ? (desc ? -1 : 1) : 0));

      const totalItems = list.length;
      const totalPages = Math.ceil(totalItems / pageSize);
      const items = list.slice((page - 1) * pageSize, (page - 1) * pageSize + pageSize).map(serializeUser);

      if (VIOLATIONS.has('bad-pagination')) {
        return send({ status: 200, contentType: 'application/json', body: { items, nextCursor: items.length === pageSize ? 'cursor-abc' : null } });
      }
      const pageBody = { page, pageSize, totalItems, totalPages, items };
      if (VIOLATIONS.has('wrong-type')) pageBody.totalItems = String(pageBody.totalItems);
      if (VIOLATIONS.has('missing-field')) delete pageBody.totalPages;
      return send({ status: 200, contentType: 'application/json', body: pageBody });
    }

    const userIdMatch = url.pathname.match(/^\/v1\/users\/([^/]+)$/);
    if (userIdMatch && (req.method === 'GET' || req.method === 'PATCH')) {
      const caller = authenticate(req);
      if (!caller) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      const target = users.get(userIdMatch[1]);
      if (!target) return send(problem({ status: 404, title: 'User not found', code: 'IDENTITY.USER_NOT_FOUND' }));
      if (req.method === 'GET') return send({ status: 200, contentType: 'application/json', body: serializeUser(target) });

      if (body && typeof body.displayName === 'string') target.displayName = body.displayName;
      if (body && typeof body.email === 'string' && body.email !== target.email) {
        if (usersByEmail.has(body.email)) return send(problem({ status: 409, title: 'Email already in use', code: 'IDENTITY.EMAIL_TAKEN' }));
        usersByEmail.delete(target.email);
        target.email = body.email;
        usersByEmail.set(body.email, target.id);
      }
      target.updatedAt = isoTimestamp();
      return send({ status: 200, contentType: 'application/json', body: serializeUser(target) });
    }

    const deactivateMatch = url.pathname.match(/^\/v1\/users\/([^/]+)\/deactivate$/);
    if (deactivateMatch && req.method === 'POST') {
      const caller = authenticate(req);
      if (!caller) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      const target = users.get(deactivateMatch[1]);
      if (!target) return send(problem({ status: 404, title: 'User not found', code: 'IDENTITY.USER_NOT_FOUND' }));
      target.status = 'inactive';
      target.updatedAt = isoTimestamp();
      return send({ status: 200, contentType: 'application/json', body: serializeUser(target) });
    }

    if (req.method === 'GET' && url.pathname === '/v1/roles') {
      const caller = authenticate(req);
      if (!caller) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      return send({ status: 200, contentType: 'application/json', body: serializeRoles(ROLES) });
    }

    const assignMatch = url.pathname.match(/^\/v1\/users\/([^/]+)\/roles$/);
    if (assignMatch && req.method === 'POST') {
      const caller = authenticate(req);
      if (!caller) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      const target = users.get(assignMatch[1]);
      const role = body && ROLES.find((r) => r.id === body.roleId);
      if (!target || !role) return send(problem({ status: 404, title: 'User or role not found', code: 'IDENTITY.NOT_FOUND' }));
      if (!target.roles.includes(role.id)) target.roles.push(role.id);
      target.updatedAt = isoTimestamp();
      return send({ status: 200, contentType: 'application/json', body: serializeUser(target) });
    }

    const revokeMatch = url.pathname.match(/^\/v1\/users\/([^/]+)\/roles\/([^/]+)$/);
    if (revokeMatch && req.method === 'DELETE') {
      const caller = authenticate(req);
      if (!caller) return send(problem({ status: 401, title: 'Unauthorized', code: 'IDENTITY.UNAUTHORIZED' }));
      const target = users.get(revokeMatch[1]);
      const roleExists = ROLES.some((r) => r.id === revokeMatch[2]);
      if (!target || !roleExists) return send(problem({ status: 404, title: 'User or role not found', code: 'IDENTITY.NOT_FOUND' }));
      target.roles = target.roles.filter((rid) => rid !== revokeMatch[2]);
      target.updatedAt = isoTimestamp();
      res.statusCode = 204;
      res.end();
      return;
    }

    send({ status: 404, contentType: 'application/json', body: { error: 'no such route in the stub', path: url.pathname, method: req.method } });
  } catch (err) {
    send({ status: 500, contentType: 'application/json', body: { error: 'stub internal error', message: String((err && err.stack) || err) } });
  }
});

server.listen(PORT, () => {
  console.log(`StackBraid conformance stub listening on http://localhost:${PORT} (access token TTL ${ACCESS_TTL_MS}ms)`);
  if (VIOLATIONS.size > 0) console.log(`Deliberately injected violations: ${[...VIOLATIONS].join(', ')}`);
});
