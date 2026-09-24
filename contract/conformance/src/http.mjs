// A deliberately thin HTTP client. No axios, no supertest, no dependency at
// all — Node's built-in fetch is enough, and a hand-rolled wrapper keeps the
// suite's own behaviour (cookie handling especially) fully auditable.

/**
 * @param {string} baseUrl
 * @param {object} options
 * @param {'GET'|'POST'|'PATCH'|'DELETE'} [options.method]
 * @param {string} options.path
 * @param {unknown} [options.body]
 * @param {Record<string,string>} [options.headers]
 * @param {string} [options.accessToken] Sent as `Authorization: Bearer <token>`.
 * @param {string} [options.cookie] Raw `Cookie` header value, e.g. `refreshToken=abc`.
 */
export async function request(baseUrl, options) {
  const { method = 'GET', path, body, headers = {}, accessToken, cookie } = options;
  const url = new URL(path, baseUrl).toString();

  const finalHeaders = { accept: 'application/json', ...headers };
  if (body !== undefined) finalHeaders['content-type'] = 'application/json';
  if (accessToken) finalHeaders['authorization'] = `Bearer ${accessToken}`;
  if (cookie) finalHeaders['cookie'] = cookie;

  const res = await fetch(url, {
    method,
    headers: finalHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  const contentType = res.headers.get('content-type') || '';
  const text = await res.text();
  let parsedBody = null;
  if (text.length > 0) {
    if (contentType.includes('json')) {
      try {
        parsedBody = JSON.parse(text);
      } catch (err) {
        parsedBody = { __parseError: String(err && err.message), __rawBody: text };
      }
    } else {
      parsedBody = { __rawBody: text };
    }
  }

  const setCookies =
    typeof res.headers.getSetCookie === 'function'
      ? res.headers.getSetCookie()
      : res.headers.get('set-cookie')
        ? [res.headers.get('set-cookie')]
        : [];

  return {
    status: res.status,
    contentType,
    headers: res.headers,
    body: parsedBody,
    rawText: text,
    setCookies,
  };
}

/** Reads one cookie's value out of a list of raw `Set-Cookie` header strings. */
export function extractCookieValue(setCookies, name) {
  for (const raw of setCookies) {
    const firstPair = raw.split(';')[0];
    const eq = firstPair.indexOf('=');
    if (eq === -1) continue;
    if (firstPair.slice(0, eq).trim() === name) return firstPair.slice(eq + 1).trim();
  }
  return null;
}

/** Reads one cookie's attributes (HttpOnly, Secure, SameSite, Path, ...) out of raw Set-Cookie strings. */
export function parseCookieAttributes(setCookies, name) {
  const raw = setCookies.find((c) => c.split('=')[0].trim() === name);
  if (!raw) return null;
  const parts = raw.split(';').map((p) => p.trim());
  const attrs = { raw };
  for (const part of parts.slice(1)) {
    const eq = part.indexOf('=');
    if (eq === -1) {
      attrs[part.toLowerCase()] = true;
    } else {
      attrs[part.slice(0, eq).toLowerCase()] = part.slice(eq + 1);
    }
  }
  return attrs;
}
