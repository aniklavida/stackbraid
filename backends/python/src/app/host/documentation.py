"""Serves the authoritative OpenAPI contract and browsable API documentation.

StackBraid is contract-first: contract/openapi.yaml is the single source of
truth. Both backends serve the exact same contract file at /openapi.yaml, and
interactive documentation at /docs driven by that contract — never generated
from backend implementation code.

Swagger UI itself is vendored at contract/docs-assets/swagger-ui and served
from this backend at /docs/assets/, so the page renders with no network egress
and no third party learns who reads these docs. See that directory's README for
the provenance record and why it is a copy rather than a CDN reference.
"""

from __future__ import annotations

import os
from pathlib import Path

from fastapi import APIRouter, HTTPException, Response
from fastapi.responses import HTMLResponse

# The only files this backend will serve out of the vendored asset directory,
# each with the content type it must be sent as. An explicit allow-list rather
# than a path join: a request for /docs/assets/../../../etc/passwd resolves to a
# name that is simply not a key here.
DOCS_ASSET_MEDIA_TYPES = {
    "swagger-ui.css": "text/css; charset=utf-8",
    "swagger-ui-bundle.js": "application/javascript; charset=utf-8",
}

DOCUMENTATION_HTML = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>StackBraid Identity API</title>
  <link rel="stylesheet" href="/docs/assets/swagger-ui.css" />
  <style>
    html {
      box-sizing: border-box;
      overflow: -moz-scrollbars-vertical;
      overflow-y: scroll;
    }
    *, *:before, *:after {
      box-sizing: inherit;
    }
    body {
      margin: 0;
      background: #fafafa;
    }
  </style>
</head>
<body>
  <div id="swagger-ui"></div>
  <script src="/docs/assets/swagger-ui-bundle.js" charset="UTF-8"></script>
  <script>
    window.onload = function() {
      SwaggerUIBundle({
        url: '/openapi.yaml',
        dom_id: '#swagger-ui',
        deepLinking: true,
        presets: [
          SwaggerUIBundle.presets.apis
        ],
        layout: 'BaseLayout',
        // Swagger UI otherwise renders a validity badge by sending the spec's
        // URL to validator.swagger.io. Nothing about this page may talk to a
        // third party.
        validatorUrl: null
      });
    };
  </script>
</body>
</html>
"""


def resolve_contract_path(configured_path: str = "") -> Path:
    """Locates the authoritative OpenAPI contract file.

    Checks:
      1. Explicit configured path or STACKBRAID_CONTRACT_PATH environment variable.
      2. Repository root relative to this file's package structure.
      3. Current working directory or parent directories.
    """
    path_str = configured_path or os.environ.get("STACKBRAID_CONTRACT_PATH", "")
    if path_str:
        candidate = Path(path_str).resolve()
        if candidate.is_file():
            return candidate
        raise FileNotFoundError(f"Configured OpenAPI contract file not found: {path_str}")

    candidates = [
        Path(__file__).resolve().parents[4] / "contract" / "openapi.yaml",
        Path.cwd() / "contract" / "openapi.yaml",
        Path.cwd().parent / "contract" / "openapi.yaml",
        Path.cwd().parents[1] / "contract" / "openapi.yaml",
    ]

    for candidate in candidates:
        if candidate.is_file():
            return candidate

    tried = "\n".join(f" - {p}" for p in candidates)
    raise FileNotFoundError(
        f"Authoritative OpenAPI contract file 'contract/openapi.yaml' could not be found. Tried:\n{tried}"
    )


def resolve_docs_assets_dir(configured_path: str = "") -> Path:
    """Locates the vendored Swagger UI asset directory.

    The assets normally sit beside the contract, in contract/docs-assets/swagger-ui.
    When the contract has been relocated by configuration they may not have moved
    with it, so the repository-relative locations are tried as well.
    """
    candidates: list[Path] = []

    try:
        candidates.append(resolve_contract_path(configured_path).parent / "docs-assets" / "swagger-ui")
    except FileNotFoundError:
        pass

    candidates.extend(
        [
            Path(__file__).resolve().parents[4] / "contract" / "docs-assets" / "swagger-ui",
            Path.cwd() / "contract" / "docs-assets" / "swagger-ui",
            Path.cwd().parent / "contract" / "docs-assets" / "swagger-ui",
        ]
    )

    for candidate in candidates:
        if candidate.is_dir():
            return candidate

    tried = "\n".join(f" - {p}" for p in candidates)
    raise FileNotFoundError(
        f"Vendored Swagger UI assets 'contract/docs-assets/swagger-ui' could not be found. Tried:\n{tried}"
    )


def get_contract_bytes(configured_path: str = "") -> bytes:
    """Reads the authoritative contract file as raw bytes."""
    path = resolve_contract_path(configured_path)
    return path.read_bytes()


def get_docs_asset_bytes(file_name: str, configured_path: str = "") -> bytes:
    """Reads one vendored Swagger UI asset as raw bytes."""
    if file_name not in DOCS_ASSET_MEDIA_TYPES:
        raise KeyError(file_name)
    return (resolve_docs_assets_dir(configured_path) / file_name).read_bytes()


def create_documentation_router(configured_path: str = "") -> APIRouter:
    router = APIRouter(include_in_schema=False)

    @router.get("/openapi.yaml")
    def serve_openapi_yaml() -> Response:
        content = get_contract_bytes(configured_path)
        return Response(content=content, media_type="application/yaml; charset=utf-8")

    @router.get("/docs")
    @router.get("/docs/")
    def serve_docs() -> HTMLResponse:
        return HTMLResponse(content=DOCUMENTATION_HTML)

    @router.get("/docs/assets/{file_name}")
    def serve_docs_asset(file_name: str) -> Response:
        media_type = DOCS_ASSET_MEDIA_TYPES.get(file_name)
        if media_type is None:
            raise HTTPException(status_code=404)
        return Response(
            content=get_docs_asset_bytes(file_name, configured_path),
            media_type=media_type,
        )

    return router
