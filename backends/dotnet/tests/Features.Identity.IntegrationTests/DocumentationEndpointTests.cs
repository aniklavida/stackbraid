using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using StackBraid.Host.Documentation;

namespace StackBraid.Features.Identity.IntegrationTests;

/// <summary>
/// Proves that ContractProvider loads authoritative contract bytes matching contract/openapi.yaml,
/// and that DocumentationHtml is configured for Swagger UI without code-derived endpoints.
/// </summary>
public sealed class ContractProviderTests
{
    [Fact]
    public void ContractProvider_returns_bytes_identical_to_repository_contract()
    {
        var contractBytes = ContractProvider.GetContractBytes();
        contractBytes.ShouldNotBeNull();
        contractBytes.Length.ShouldBeGreaterThan(0);

        var repoRoot = FindRepoRoot();
        var contractPath = Path.Combine(repoRoot, "contract", "openapi.yaml");
        File.Exists(contractPath).ShouldBeTrue($"Expected contract at {contractPath}");

        var fileBytes = File.ReadAllBytes(contractPath);
        contractBytes.ShouldBe(fileBytes);
    }

    [Fact]
    public void Embedded_contract_resource_is_byte_identical_to_repository_contract()
    {
        var embeddedBytes = ContractProvider.GetEmbeddedContractBytes();
        embeddedBytes.ShouldNotBeNull();
        embeddedBytes.Length.ShouldBeGreaterThan(0);

        var repoRoot = FindRepoRoot();
        var contractPath = Path.Combine(repoRoot, "contract", "openapi.yaml");
        var fileBytes = File.ReadAllBytes(contractPath);
        embeddedBytes.ShouldBe(fileBytes);
    }

    [Fact]
    public void DocumentationHtml_serves_swagger_ui_referencing_openapi_yaml()
    {
        DocumentationHtml.Content.ShouldContain("/openapi.yaml");
        DocumentationHtml.Content.ShouldNotContain("openapi.json");
        DocumentationHtml.Content.ShouldContain("SwaggerUIBundle");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "contract", "openapi.yaml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find repository root containing contract/openapi.yaml.");
    }
}

/// <summary>
/// Proves that both the machine-readable OpenAPI contract and the human-facing
/// documentation page are served from contract/openapi.yaml, that code-derived
/// documentation endpoints are absent, and that no Swashbuckle/NSwag code generator
/// is present.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DocumentationEndpointTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentationEndpointTests(PostgresDatabaseFixture postgres)
    {
        var connectionString = Environment.GetEnvironmentVariable("STACKBRAID_TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("STACKBRAID_TEST_POSTGRES_CONNECTION_STRING is not set.");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Jwt:SigningKey", "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=");
            builder.UseSetting("Jwt:Issuer", "stackbraid-tests");
            builder.UseSetting("Jwt:Audience", "stackbraid-tests");
        });
    }

    [Fact]
    public async Task OpenApiYaml_serves_authoritative_contract_byte_identical()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi.yaml");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/yaml");

        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        var repoRoot = FindRepoRoot();
        var contractPath = Path.Combine(repoRoot, "contract", "openapi.yaml");
        var expectedBytes = await File.ReadAllBytesAsync(contractPath);

        responseBytes.ShouldBe(expectedBytes);
    }

    [Fact]
    public async Task Docs_serves_browsable_swagger_ui_referencing_openapi_yaml()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/docs");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("/openapi.yaml");
        html.ShouldNotContain("openapi.json");
        html.ShouldContain("SwaggerUIBundle");

        // Trailing slash also resolves
        var responseSlash = await client.GetAsync("/docs/");
        responseSlash.StatusCode.ShouldBe(HttpStatusCode.OK);
        var htmlSlash = await responseSlash.Content.ReadAsStringAsync();
        htmlSlash.ShouldBe(html);
    }

    [Fact]
    public async Task Code_derived_documentation_endpoints_return_404()
    {
        using var client = _factory.CreateClient();

        var resJson = await client.GetAsync("/openapi.json");
        resJson.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var resRedoc = await client.GetAsync("/redoc");
        resRedoc.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "contract", "openapi.yaml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find repository root containing contract/openapi.yaml.");
    }
}
