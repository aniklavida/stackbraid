namespace StackBraid.Host.Documentation;

/// <summary>
/// Provides the browsable Swagger UI HTML page. Driven entirely by the
/// authoritative OpenAPI contract served at /openapi.yaml — never generated
/// from backend code.
///
/// Swagger UI itself is vendored at contract/docs-assets/swagger-ui and served
/// from this backend at /docs/assets/, so the page renders with no network
/// egress and no third party learns who reads these docs. See that directory's
/// README for the provenance record and why it is a copy rather than a CDN
/// reference.
/// </summary>
public static class DocumentationHtml
{
    public const string Content = """
<!DOCTYPE html>
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

""";
}
