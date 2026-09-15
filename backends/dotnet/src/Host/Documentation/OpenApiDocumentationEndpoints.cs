namespace StackBraid.Host.Documentation;

/// <summary>
/// Mounts endpoints serving the authoritative OpenAPI specification at /openapi.yaml
/// and the browsable Swagger UI documentation at /docs.
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

        return app;
    }
}
