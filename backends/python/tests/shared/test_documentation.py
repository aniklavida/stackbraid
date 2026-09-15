"""Proves the documentation surface is contract-driven and code-derived docs are removed.

FastAPI defaults to generating OpenAPI specs from Python code at /openapi.json,
Swagger UI at /docs, and Redoc at /redoc. StackBraid is contract-first, so those
code-derived endpoints must be completely disabled, and replaced by endpoints
serving the authoritative contract/openapi.yaml file and contract-driven browsable UI.
"""

import re
from pathlib import Path

from fastapi.testclient import TestClient

from app.host.main import create_app


def test_fastapi_code_derived_docs_are_disabled() -> None:
    app = create_app()
    assert app.docs_url is None
    assert app.redoc_url is None
    assert app.openapi_url is None


def test_code_derived_docs_endpoints_return_404() -> None:
    app = create_app()
    client = TestClient(app)

    res_json = client.get("/openapi.json")
    assert res_json.status_code == 404

    res_redoc = client.get("/redoc")
    assert res_redoc.status_code == 404


def test_openapi_yaml_serves_authoritative_contract_byte_identical() -> None:
    app = create_app()
    client = TestClient(app)

    repo_root = Path(__file__).resolve().parents[4]
    contract_file = repo_root / "contract" / "openapi.yaml"
    assert contract_file.is_file(), f"Contract file missing at {contract_file}"
    expected_bytes = contract_file.read_bytes()

    res = client.get("/openapi.yaml")
    assert res.status_code == 200
    assert "application/yaml" in res.headers["content-type"]
    assert res.content == expected_bytes


def test_docs_serves_browsable_ui_referencing_openapi_yaml() -> None:
    app = create_app()
    client = TestClient(app)

    res = client.get("/docs")
    assert res.status_code == 200
    assert "text/html" in res.headers["content-type"]

    html = res.text
    # Driven by the contract, never code-derived openapi.json
    assert "/openapi.yaml" in html
    assert "openapi.json" not in html
    assert "SwaggerUIBundle" in html

    # Trailing slash also resolves
    res_slash = client.get("/docs/")
    assert res_slash.status_code == 200
    assert res_slash.text == html


def test_docs_page_references_no_third_party_url() -> None:
    """The documentation must render with no route to the internet.

    Swagger UI is vendored and served from this backend. An absolute URL in the
    page — a CDN script or stylesheet, or Swagger UI's own validator badge
    calling validator.swagger.io — would both break the page offline and tell a
    third party who is reading these docs.
    """
    app = create_app()
    client = TestClient(app)

    html = client.get("/docs").text
    external = sorted(set(re.findall(r"https?://[^\s\"'<>()]+", html)))
    assert external == [], f"documentation page references external URLs: {external}"


def test_docs_assets_are_served_byte_identical_to_the_vendored_files() -> None:
    app = create_app()
    client = TestClient(app)

    vendored_dir = Path(__file__).resolve().parents[4] / "contract" / "docs-assets" / "swagger-ui"
    expected_content_types = {
        "swagger-ui.css": "text/css",
        "swagger-ui-bundle.js": "javascript",
    }

    for file_name, expected_type in expected_content_types.items():
        vendored_file = vendored_dir / file_name
        assert vendored_file.is_file(), f"Vendored Swagger UI file missing at {vendored_file}"

        res = client.get(f"/docs/assets/{file_name}")
        assert res.status_code == 200
        assert expected_type in res.headers["content-type"]
        assert res.content == vendored_file.read_bytes()


def test_unknown_docs_asset_returns_404() -> None:
    """The asset route is an allow-list, not a path join over the vendored directory.

    "LICENSE" sits in that directory and must still not be served;
    "swagger-ui-standalone-preset.js" is a real file of the package we
    deliberately did not vendor.
    """
    app = create_app()
    client = TestClient(app)

    for attempt in ("not-a-real-asset.js", "LICENSE", "swagger-ui-standalone-preset.js"):
        assert client.get(f"/docs/assets/{attempt}").status_code == 404
