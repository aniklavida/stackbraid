# Vendored Swagger UI

These files are **not ours**. They are the browser assets of
[`swagger-ui-dist`](https://www.npmjs.com/package/swagger-ui-dist), copied here byte-identical
from the published npm package and served by both backends at `/docs`.

`PROVENANCE.json` records the package, version, licence, the npm tarball integrity hash, and a
SHA-256 for every file. `scripts/check-dependency-licenses.mjs` re-hashes them on every CI run.
**Never hand-edit a file in this directory** — a single changed byte fails the build, which is
the point.

## Why vendored rather than loaded from a CDN

A CDN `<script>` tag would be one line instead of 1.7 MB in the tree. We took the 1.7 MB because:

- **The documentation works offline.** A backend on an air-gapped network, a laptop on a plane,
  a CI container with no egress — `/docs` renders. A CDN reference turns the page blank there.
- **No third party is told who reads our API docs.** A CDN sees the IP and referrer of every
  developer who opens the page.
- **Nothing executes that we did not audit.** With a CDN, the bytes the browser runs are whatever
  the CDN serves at request time. Subresource integrity narrows that to "the bytes we pinned, or
  nothing at all" — but the request still leaves the network, and the page still breaks when it
  fails. Vendoring removes the question.

The cost, stated plainly: upgrading Swagger UI is now a deliberate commit rather than something
that happens on its own. That is a trade we chose, not one we overlooked.

## Upgrading

```bash
npm pack swagger-ui-dist@<version>          # npm verifies the tarball integrity on download
tar -xzf swagger-ui-dist-<version>.tgz
cp package/{swagger-ui.css,swagger-ui-bundle.js,swagger-ui-bundle.js.LICENSE.txt,LICENSE,NOTICE} \
   contract/docs-assets/swagger-ui/
```

Then, in the same commit:

1. Update `version`, `source`, `tarballIntegrity`, `retrievedOn` and every `files` hash in
   `PROVENANCE.json` — `npm view swagger-ui-dist@<version> dist.integrity` and
   `shasum -a 256 <file>` give you the values.
2. Update the `vendored-browser-asset` entry in `docs/dependency-inventory.json` and the
   corresponding row in `docs/DEPENDENCIES.md`, re-verifying the licence from the package's own
   `LICENSE` file rather than carrying the old record forward.
3. Run `node scripts/check-dependency-licenses.mjs` and the contract conformance suite.

## What is deliberately not here

- `swagger-ui-standalone-preset.js` — it supplies the topbar and its "explore any URL" box. The
  page uses `BaseLayout`, which does not need the preset, and we do not want a box inviting people
  to point our viewer at an arbitrary spec.
- `*.map` source maps — 4 MB of debugging aid for code we do not debug. Browser devtools will log
  a 404 for `swagger-ui.css.map`; nothing else notices.
- The ES-module bundles, `index.html`, `swagger-initializer.js` and the favicons — each backend
  serves its own page.

## Licence

Apache-2.0. The full text is in `LICENSE`, the copyright notice in `NOTICE`, and the notices for
the third-party code bundled inside `swagger-ui-bundle.js` in `swagger-ui-bundle.js.LICENSE.txt`.
All three are served nowhere and shipped everywhere — they travel with the copy, as the licence
requires.
