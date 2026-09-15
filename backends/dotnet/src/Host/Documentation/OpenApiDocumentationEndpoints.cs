namespace StackBraid.Host.Documentation;

/// <summary>
/// Mounts endpoints serving the authoritative OpenAPI specification at /openapi.yaml,
/// the browsable Swagger UI documentation at /docs, and the vendored Swagger UI
/// browser assets that page loads at /docs/assets/.
/// </summary>
public static class OpenApiDocumentationEndpoints
{
    public static IEndpointRouteBuilder MapOpenApiDocumentation(this IEndpointRouteBuilder app)
    {
        app.MapGet("/openapi.yaml", (IConfiguration configuration) =>
        {
            var bytes = ContractProvider.GetContractBytes(configuration);
            return Results.File(bytes, "application/yaml; charset=utf-8");
        });

        app.MapGet("/docs", () => Results.Content(DocumentationHtml.Content, "text/html; charset=utf-8"));

        app.MapGet("/docs/assets/{fileName}", (string fileName) =>
            DocumentationAssetProvider.TryGetAsset(fileName, out var bytes, out var mediaType)
                ? Results.File(bytes, mediaType)
                : Results.NotFound());

        return app;
    }
}
