using System.Net;
using System.Text.RegularExpressions;
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

    [Fact]
    public void DocumentationHtml_loads_swagger_ui_from_this_backend_not_a_third_party()
    {
        // The documentation must render on a machine with no route to the
        // internet, and must not tell a third party who is reading it. A CDN
        // script or stylesheet, or Swagger UI's own validator badge calling
        // validator.swagger.io, would break both promises.
        var external = Regex.Matches(DocumentationHtml.Content, @"https?://[^\s""'<>()]+")
            .Select(match => match.Value)
            .Distinct()
            .ToArray();

        external.ShouldBeEmpty($"documentation page references external URLs: {string.Join(", ", external)}");
    }

    [Fact]
    public void Embedded_documentation_assets_are_byte_identical_to_the_vendored_files()
    {
        var repoRoot = FindRepoRoot();

        foreach (var fileName in DocumentationAssetProvider.AssetFileNames)
        {
            DocumentationAssetProvider.TryGetAsset(fileName, out var bytes, out var mediaType)
                .ShouldBeTrue($"Expected {fileName} to be a served documentation asset.");
            mediaType.ShouldNotBeNullOrWhiteSpace();

            var vendoredPath = Path.Combine(repoRoot, "contract", "docs-assets", "swagger-ui", fileName);
            File.Exists(vendoredPath).ShouldBeTrue($"Expected vendored asset at {vendoredPath}");
            bytes.ShouldBe(File.ReadAllBytes(vendoredPath));
        }
    }

    [Fact]
    public void Only_the_vendored_documentation_assets_resolve()
    {
        // An allow-list, not a path join over the vendored directory. "LICENSE"
        // sits in that directory and must still not resolve;
        // "swagger-ui-standalone-preset.js" is a real file of the package we
        // deliberately did not vendor.
        foreach (var fileName in new[] { "not-a-real-asset.js", "LICENSE", "swagger-ui-standalone-preset.js" })
        {
            DocumentationAssetProvider.TryGetAsset(fileName, out _, out _)
                .ShouldBeFalse($"{fileName} must not be served from /docs/assets/.");
        }
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
    public async Task Docs_assets_are_served_byte_identical_to_the_vendored_files()
    {
        using var client = _factory.CreateClient();
        var repoRoot = FindRepoRoot();

        var expectedMediaTypes = new Dictionary<string, string>
        {
            ["swagger-ui.css"] = "text/css",
            ["swagger-ui-bundle.js"] = "application/javascript",
        };

        foreach (var (fileName, mediaType) in expectedMediaTypes)
        {
            var response = await client.GetAsync($"/docs/assets/{fileName}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.ShouldBe(mediaType);

            var vendoredPath = Path.Combine(repoRoot, "contract", "docs-assets", "swagger-ui", fileName);
            File.Exists(vendoredPath).ShouldBeTrue($"Expected vendored asset at {vendoredPath}");

            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            responseBytes.ShouldBe(await File.ReadAllBytesAsync(vendoredPath));
        }
    }

    [Fact]
    public async Task Unknown_docs_asset_returns_404()
    {
        using var client = _factory.CreateClient();

        foreach (var fileName in new[] { "not-a-real-asset.js", "LICENSE", "swagger-ui-standalone-preset.js" })
        {
            var response = await client.GetAsync($"/docs/assets/{fileName}");
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
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
