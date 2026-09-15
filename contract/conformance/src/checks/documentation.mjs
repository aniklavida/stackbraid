// Proves that both backends serve the authoritative contract/openapi.yaml
// specification byte-identical at /openapi.yaml, that browsable interactive
// documentation is served at /docs referencing /openapi.yaml, that the page
// loads Swagger UI from the backend's own vendored copy rather than from any
// third party, and that code-derived documentation endpoints (/openapi.json,
// /redoc) are absent.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { request } from '../http.mjs';
import { fail } from '../assert.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CONTRACT_FILE_PATH = path.resolve(__dirname, '../../../openapi.yaml');
const DOCS_ASSETS_DIR = path.resolve(__dirname, '../../../docs-assets/swagger-ui');

// Every file the documentation page pulls in, and the content type it must
// arrive as. A backend that serves one of these from anywhere but its own
// vendored copy fails the byte-equality check below.
const DOCS_ASSETS = [
  { fileName: 'swagger-ui.css', contentType: 'text/css' },
  { fileName: 'swagger-ui-bundle.js', contentType: 'javascript' },
];

export async function registerDocumentationChecks(harness, ctx) {
  await harness.run('GET /openapi.yaml — serves the authoritative OpenAPI contract byte-identical', async () => {
    const res = await request(ctx.baseUrl, { path: '/openapi.yaml', headers: { accept: '*/*' } });
    if (res.status !== 200) {
      fail('expected GET /openapi.yaml to return 200', { field: 'status', expected: 200, actual: res.status });
    }

    if (!res.contentType.includes('yaml')) {
      fail('expected Content-Type of /openapi.yaml to be YAML', {
        field: 'content-type',
        expected: 'application/yaml; charset=utf-8',
        actual: res.contentType,
      });
    }

    if (!fs.existsSync(CONTRACT_FILE_PATH)) {
      fail(`Authoritative contract file missing at expected path: ${CONTRACT_FILE_PATH}`);
    }

    const expectedText = fs.readFileSync(CONTRACT_FILE_PATH, 'utf-8');
    const actualText = res.rawText || '';

    if (actualText !== expectedText) {
      fail('served /openapi.yaml is not byte-identical to contract/openapi.yaml', {
        field: 'body',
        expected: `byte length ${Buffer.byteLength(expectedText)}`,
        actual: `byte length ${Buffer.byteLength(actualText)}`,
      });
    }
  });

  await harness.run('GET /docs — serves browsable API documentation driven by /openapi.yaml', async () => {
    const res = await request(ctx.baseUrl, { path: '/docs', headers: { accept: 'text/html' } });
    if (res.status !== 200) {
      fail('expected GET /docs to return 200', { field: 'status', expected: 200, actual: res.status });
    }

    if (!res.contentType.includes('text/html')) {
      fail('expected Content-Type of /docs to be text/html', {
        field: 'content-type',
        expected: 'text/html; charset=utf-8',
        actual: res.contentType,
      });
    }

    const html = res.rawText || '';
    if (!html.includes('/openapi.yaml')) {
      fail('documentation page does not load the authoritative /openapi.yaml contract', {
        field: 'html',
        expected: 'to contain reference to /openapi.yaml',
        actual: 'missing /openapi.yaml reference',
      });
    }

    if (html.includes('openapi.json')) {
      fail('documentation page references code-derived openapi.json instead of contract/openapi.yaml', {
        field: 'html',
        expected: 'no reference to openapi.json',
        actual: 'contains openapi.json',
      });
    }

    if (!html.includes('SwaggerUIBundle')) {
      fail('documentation page does not contain SwaggerUIBundle', {
        field: 'html',
        expected: 'to contain SwaggerUIBundle',
        actual: 'missing SwaggerUIBundle',
      });
    }

    // The documentation must render on a machine with no route to the internet,
    // and must not tell a third party who is reading it. Any absolute URL in the
    // page — a CDN script, a stylesheet, Swagger UI's own validator badge —
    // breaks both promises, so none are permitted at all.
    const externalUrls = [...new Set(html.match(/https?:\/\/[^\s"'<>()]+/g) || [])];
    if (externalUrls.length > 0) {
      fail('documentation page references external URLs; Swagger UI must be served from the backend itself', {
        field: 'html',
        expected: 'no absolute http(s) URL in the page',
        actual: externalUrls.join(', '),
      });
    }

    // Trailing slash resolves identically
    const resSlash = await request(ctx.baseUrl, { path: '/docs/', headers: { accept: 'text/html' } });
    if (resSlash.status !== 200) {
      fail('expected GET /docs/ to return 200', { field: 'status', expected: 200, actual: resSlash.status });
    }
    if ((resSlash.rawText || '') !== html) {
      fail('GET /docs/ served different content than GET /docs', {
        field: 'body',
        expected: 'identical to /docs',
        actual: 'differed',
      });
    }
  });

  await harness.run('Code-derived documentation endpoints are absent (404)', async () => {
    const [resJson, resRedoc] = await Promise.all([
      request(ctx.baseUrl, { path: '/openapi.json' }),
      request(ctx.baseUrl, { path: '/redoc' }),
    ]);

    if (resJson.status !== 404) {
      fail('code-derived /openapi.json endpoint must not exist', {
        field: '/openapi.json status',
        expected: 404,
        actual: resJson.status,
      });
    }

    if (resRedoc.status !== 404) {
      fail('code-derived /redoc endpoint must not exist', {
        field: '/redoc status',
        expected: 404,
        actual: resRedoc.status,
      });
    }
  });

  for (const asset of DOCS_ASSETS) {
    await harness.run(`GET /docs/assets/${asset.fileName} — serves the vendored Swagger UI file byte-identical`, async () => {
      const res = await request(ctx.baseUrl, { path: `/docs/assets/${asset.fileName}`, headers: { accept: '*/*' } });
      if (res.status !== 200) {
        fail(`expected GET /docs/assets/${asset.fileName} to return 200`, { field: 'status', expected: 200, actual: res.status });
      }

      if (!res.contentType.includes(asset.contentType)) {
        fail(`expected Content-Type of /docs/assets/${asset.fileName} to be ${asset.contentType}`, {
          field: 'content-type',
          expected: asset.contentType,
          actual: res.contentType,
        });
      }

      const vendoredPath = path.join(DOCS_ASSETS_DIR, asset.fileName);
      if (!fs.existsSync(vendoredPath)) {
        fail(`Vendored Swagger UI file missing at expected path: ${vendoredPath}`);
      }

      const expectedText = fs.readFileSync(vendoredPath, 'utf-8');
      const actualText = res.rawText || '';
      if (actualText !== expectedText) {
        fail(`served /docs/assets/${asset.fileName} is not byte-identical to contract/docs-assets/swagger-ui/${asset.fileName}`, {
          field: 'body',
          expected: `byte length ${Buffer.byteLength(expectedText)}`,
          actual: `byte length ${Buffer.byteLength(actualText)}`,
        });
      }
    });
  }

  await harness.run('GET /docs/assets/<unknown> — only the vendored files are reachable', async () => {
    // The asset route must be an allow-list, not a path join over the vendored
    // directory. 'LICENSE' sits in that directory and must still not be served;
    // 'swagger-ui-standalone-preset.js' is a real file of the package we
    // deliberately did not vendor.
    for (const attempt of ['not-a-real-asset.js', 'LICENSE', 'swagger-ui-standalone-preset.js']) {
      const res = await request(ctx.baseUrl, { path: `/docs/assets/${attempt}` });
      if (res.status !== 404) {
        fail(`expected /docs/assets/${attempt} to return 404`, {
          field: 'status',
          expected: 404,
          actual: res.status,
        });
      }
    }
  });
}
