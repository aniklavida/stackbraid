"""Serves the authoritative OpenAPI contract and browsable API documentation.

StackBraid is contract-first: contract/openapi.yaml is the single source of
truth. Both backends serve the exact same contract file at /openapi.yaml, and
interactive documentation at /docs driven by that contract — never generated
from backend implementation code.
"""

from __future__ import annotations

import os
from pathlib import Path

from fastapi import APIRouter, Response
from fastapi.responses import HTMLResponse

DOCUMENTATION_HTML = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>StackBraid Identity API</title>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5.18.2/swagger-ui.css" />
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
  <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5.18.2/swagger-ui-bundle.js" charset="UTF-8"></script>
  <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5.18.2/swagger-ui-standalone-preset.js" charset="UTF-8"></script>
  <script>
    window.onload = function() {
      SwaggerUIBundle({
        url: '/openapi.yaml',
        dom_id: '#swagger-ui',
        deepLinking: true,
        presets: [
          SwaggerUIBundle.presets.apis,
          SwaggerUIStandalonePreset
        ],
        layout: 'BaseLayout'
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


def get_contract_bytes(configured_path: str = "") -> bytes:
    """Reads the authoritative contract file as raw bytes."""
    path = resolve_contract_path(configured_path)
    return path.read_bytes()


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

    return router
