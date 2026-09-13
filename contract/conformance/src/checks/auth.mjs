// Auth: register, login, refresh, logout, me.
//
// Card 1 resolved an ambiguity this file exists to prove both halves of:
// `TokenPair` returns both tokens in the body (mobile/API clients), AND
// login/refresh/logout also carry the refresh token as an httpOnly cookie
// (browser clients). Every one of those three endpoints is exercised via
// both paths below, not just login.
//
// The three auth invariants the card asks for — a refreshed token works, an
// expired one does not, a revoked one does not — are each covered at least
// twice: once through rotation (the old token from a refresh is revoked)
// and once through logout (an explicit revocation).

import { request, extractCookieValue, parseCookieAttributes } from '../http.mjs';
import { fail } from '../assert.mjs';
import { Skip } from '../report.mjs';
import { validateUser, validateTokenPair, violationsMessage } from '../schema.mjs';
import { expectProblem, uniqueEmail } from './shared.mjs';

const PASSWORD = 'Conformance!2026';
const DEFAULT_MAX_EXPIRY_WAIT_MS = 65_000;
const EXPIRY_BUFFER_MS = 1_500;

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function registerAuthChecks(harness, ctx) {
  const primaryEmail = uniqueEmail('primary');
  ctx.primaryUser = { email: primaryEmail, password: PASSWORD };

  await harness.run('POST /v1/auth/register — creates an account (201, User shape)', async () => {
    const res = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/register',
      body: { email: primaryEmail, password: PASSWORD, displayName: 'Conformance Primary' },
    });
    if (res.status !== 201) fail('unexpected status registering a new account', { field: 'status', expected: 201, actual: res.status });
    const violations = [];
    validateUser(res.body, 'body', violations);
    if (violations.length) fail(violationsMessage('User shape after register', violations));
    if (res.body.email !== primaryEmail) {
      fail('registered user echoes the wrong email', { field: 'body.email', expected: primaryEmail, actual: res.body.email });
    }
    ctx.primaryUser.id = res.body.id;
  });

  await harness.run('POST /v1/auth/register — duplicate email is rejected (409, Problem)', async () => {
    const res = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/register',
      body: { email: primaryEmail, password: PASSWORD, displayName: 'Duplicate' },
    });
    await expectProblem(res, { status: 409, context: 'duplicate registration' });
  });

  await harness.run('POST /v1/auth/register — validation failure surfaces field errors (400, Problem.errors)', async () => {
    const res = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/register',
      body: { email: 'not-an-email', password: 'short', displayName: '' },
    });
    const body = await expectProblem(res, { status: 400, expectErrors: true, context: 'invalid registration payload' });
    for (const field of ['email', 'password']) {
      const messages = body.errors && body.errors[field];
      if (!Array.isArray(messages) || messages.length === 0) {
        fail('validation Problem is missing field-level messages', {
          field: `body.errors.${field}`,
          expected: 'a non-empty array of violation messages',
          actual: messages,
        });
      }
    }
  });

  await harness.run('POST /v1/auth/login — issues a TokenPair and a browser cookie carrying the same refresh token', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: primaryEmail, password: PASSWORD } });
    if (res.status !== 200) fail('login with correct credentials was rejected', { field: 'status', expected: 200, actual: res.status });

    const violations = [];
    validateTokenPair(res.body, 'body', violations);
    if (violations.length) fail(violationsMessage('TokenPair shape after login', violations));

    const cookieValue = extractCookieValue(res.setCookies, 'refreshToken');
    if (cookieValue === null) {
      fail('login did not set a refreshToken cookie for browser clients', {
        field: 'Set-Cookie',
        expected: 'a refreshToken=<value> cookie',
        actual: res.setCookies.join(' | ') || '(no Set-Cookie header)',
      });
    }
    const attrs = parseCookieAttributes(res.setCookies, 'refreshToken');
    if (!attrs.httponly) fail('refreshToken cookie is missing HttpOnly', { field: 'Set-Cookie.HttpOnly', expected: true, actual: false });
    if (!attrs.secure) fail('refreshToken cookie is missing Secure', { field: 'Set-Cookie.Secure', expected: true, actual: false });
    if (!/^strict$/i.test(String(attrs.samesite ?? ''))) {
      fail('refreshToken cookie has the wrong SameSite attribute', { field: 'Set-Cookie.SameSite', expected: 'Strict', actual: attrs.samesite ?? '(absent)' });
    }
    if (cookieValue !== res.body.refreshToken) {
      fail('the refreshToken cookie does not carry the same token as the response body', {
        field: 'Set-Cookie(refreshToken) vs body.refreshToken',
        expected: res.body.refreshToken,
        actual: cookieValue,
      });
    }

    ctx.primaryUser.tokens = res.body;
  });

  await harness.run('POST /v1/auth/login — wrong password is rejected (401, Problem)', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: primaryEmail, password: 'wrong-password' } });
    await expectProblem(res, { status: 401, context: 'login with wrong password' });
  });

  await harness.run('GET /v1/auth/me — a freshly issued access token works', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/auth/me', accessToken: ctx.primaryUser.tokens.accessToken });
    if (res.status !== 200) fail('a freshly issued access token was rejected', { field: 'status', expected: 200, actual: res.status });
    const violations = [];
    validateUser(res.body, 'body', violations);
    if (violations.length) fail(violationsMessage('User shape from /v1/auth/me', violations));
    if (res.body.id !== ctx.primaryUser.id) fail('/v1/auth/me returned the wrong account', { field: 'body.id', expected: ctx.primaryUser.id, actual: res.body.id });
  });

  await harness.run('GET /v1/auth/me — missing token is rejected (401, Problem)', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/auth/me' });
    await expectProblem(res, { status: 401, context: 'unauthenticated /v1/auth/me' });
  });

  await harness.run('GET /v1/auth/me — malformed token is rejected (401, Problem)', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/auth/me', accessToken: 'not-a-real-token' });
    await expectProblem(res, { status: 401, context: '/v1/auth/me with a garbage bearer token' });
  });

  // --- refresh: body path (mobile / API clients — Flutter has no ambient cookie jar) ---
  let bodyPathOldRefreshToken;
  await harness.run('POST /v1/auth/refresh — body path rotates the pair (mobile/API clients)', async () => {
    bodyPathOldRefreshToken = ctx.primaryUser.tokens.refreshToken;
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/refresh', body: { refreshToken: bodyPathOldRefreshToken } });
    if (res.status !== 200) fail('refresh with a valid body refreshToken was rejected', { field: 'status', expected: 200, actual: res.status });
    const violations = [];
    validateTokenPair(res.body, 'body', violations);
    if (violations.length) fail(violationsMessage('TokenPair shape after body-path refresh', violations));
    if (res.body.refreshToken === bodyPathOldRefreshToken) {
      fail('refresh did not rotate the refresh token', { field: 'body.refreshToken', expected: 'a new value, different from the token presented', actual: res.body.refreshToken });
    }
    ctx.primaryUser.tokens = res.body;
  });

  await harness.run('GET /v1/auth/me — a refreshed access token works', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/auth/me', accessToken: ctx.primaryUser.tokens.accessToken });
    if (res.status !== 200) fail('the access token issued by a refresh was rejected', { field: 'status', expected: 200, actual: res.status });
  });

  await harness.run('POST /v1/auth/refresh — a rotated-out (revoked) body refreshToken no longer works', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/refresh', body: { refreshToken: bodyPathOldRefreshToken } });
    await expectProblem(res, { status: 401, context: 'reuse of a rotated-out refresh token' });
  });

  // --- refresh: cookie path (browser clients — "omit the body entirely to fall back to the cookie") ---
  let cookieValueBefore;
  await harness.run('POST /v1/auth/login — a separate session, to exercise the cookie path independently', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: primaryEmail, password: PASSWORD } });
    if (res.status !== 200) fail('login for the cookie-path scenario failed', { field: 'status', expected: 200, actual: res.status });
    cookieValueBefore = extractCookieValue(res.setCookies, 'refreshToken');
    if (cookieValueBefore === null) fail('login did not set a refreshToken cookie', { field: 'Set-Cookie', expected: 'refreshToken=<value>', actual: '(none)' });
  });

  await harness.run('POST /v1/auth/refresh — cookie path rotates the pair with no request body (browser clients)', async () => {
    const res = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/refresh',
      cookie: `refreshToken=${cookieValueBefore}`,
      // Deliberately no body: this is the fallback-to-cookie path the contract describes.
    });
    if (res.status !== 200) fail('cookie-path refresh (no body) was rejected', { field: 'status', expected: 200, actual: res.status });
    const violations = [];
    validateTokenPair(res.body, 'body', violations);
    if (violations.length) fail(violationsMessage('TokenPair shape after cookie-path refresh', violations));
    const newCookieValue = extractCookieValue(res.setCookies, 'refreshToken');
    if (newCookieValue === null) fail('cookie-path refresh did not re-issue the refreshToken cookie', { field: 'Set-Cookie', expected: 'refreshToken=<new value>', actual: '(none)' });
    if (newCookieValue === cookieValueBefore) fail('cookie-path refresh did not rotate the cookie value', { field: 'Set-Cookie(refreshToken)', expected: 'a new value', actual: newCookieValue });
  });

  await harness.run('POST /v1/auth/refresh — a rotated-out (revoked) refreshToken cookie no longer works', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/refresh', cookie: `refreshToken=${cookieValueBefore}` });
    await expectProblem(res, { status: 401, context: 'reuse of a rotated-out refresh cookie' });
  });

  // --- expiry: time-bounded, driven only by the contract-shaped `expiresAt` the backend itself returned ---
  await harness.run('GET /v1/auth/me — an expired access token no longer works', async () => {
    const tokens = ctx.primaryUser.tokens;
    const expiresAt = new Date(tokens.expiresAt).getTime();
    const maxWait = Number(process.env.CONFORMANCE_MAX_EXPIRY_WAIT_MS ?? DEFAULT_MAX_EXPIRY_WAIT_MS);
    const wait = expiresAt - Date.now();
    if (wait > maxWait) {
      throw new Skip(
        `access token TTL is ${(wait / 1000).toFixed(1)}s, longer than the ${(maxWait / 1000).toFixed(1)}s this run is willing to wait. ` +
          'Configure the backend under test with a short-lived access token TTL for this run (or raise CONFORMANCE_MAX_EXPIRY_WAIT_MS) to exercise this check.',
      );
    }
    if (wait > 0) await sleep(wait + EXPIRY_BUFFER_MS);
    const res = await request(ctx.baseUrl, { path: '/v1/auth/me', accessToken: tokens.accessToken });
    await expectProblem(res, { status: 401, context: 'expired access token' });
  });

  // --- logout / explicit revocation — note /v1/auth/logout, unlike its siblings, keeps the
  // global bearerAuth requirement (no `security: []` override in the contract): "revoke the
  // current session" presumes a session, so a missing access token is itself a 401. ---
  await harness.run('POST /v1/auth/logout — missing access token is rejected (401, Problem)', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/logout' });
    await expectProblem(res, { status: 401, context: 'logout without an access token' });
  });

  await harness.run('POST /v1/auth/logout — revokes the session (body path); that refreshToken then fails', async () => {
    const login = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: primaryEmail, password: PASSWORD } });
    if (login.status !== 200) fail('login before the logout body-path check failed', { field: 'status', expected: 200, actual: login.status });
    const logout = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/logout',
      accessToken: login.body.accessToken,
      body: { refreshToken: login.body.refreshToken },
    });
    if (logout.status !== 204) fail('logout with an explicit refreshToken did not return 204', { field: 'status', expected: 204, actual: logout.status });

    const reuse = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/refresh', body: { refreshToken: login.body.refreshToken } });
    await expectProblem(reuse, { status: 401, context: 'refresh with a logged-out (revoked) token' });
  });

  await harness.run('POST /v1/auth/logout — is idempotent (logging out twice is not an error)', async () => {
    const login = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: primaryEmail, password: PASSWORD } });
    const first = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/logout',
      accessToken: login.body.accessToken,
      body: { refreshToken: login.body.refreshToken },
    });
    if (first.status !== 204) fail('first logout did not return 204', { field: 'status', expected: 204, actual: first.status });
    const second = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/logout',
      accessToken: login.body.accessToken,
      body: { refreshToken: login.body.refreshToken },
    });
    if (second.status !== 204) fail('logging out an already-revoked session did not return 204 (not idempotent)', { field: 'status', expected: 204, actual: second.status });
  });

  await harness.run('POST /v1/auth/logout — cookie path revokes the session with no request body', async () => {
    const login = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: primaryEmail, password: PASSWORD } });
    const cookieValue = extractCookieValue(login.setCookies, 'refreshToken');
    const logout = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/logout', accessToken: login.body.accessToken, cookie: `refreshToken=${cookieValue}` });
    if (logout.status !== 204) fail('cookie-path logout did not return 204', { field: 'status', expected: 204, actual: logout.status });
    const reuse = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/refresh', cookie: `refreshToken=${cookieValue}` });
    await expectProblem(reuse, { status: 401, context: 'refresh with a cookie revoked by logout' });
  });

  // Hand a definitely-live session to the rest of the suite (users/roles checks).
  await harness.run('POST /v1/auth/login — session handed off to the users/roles checks', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: primaryEmail, password: PASSWORD } });
    if (res.status !== 200) fail('final login handoff failed', { field: 'status', expected: 200, actual: res.status });
    ctx.primaryUser.activeTokens = res.body;
  });
}
