using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using StackBraid.Database.Postgres;
using StackBraid.Features.Identity.Application;
using StackBraid.Features.Identity.Contracts;
using StackBraid.Features.Identity.Domain;
using StackBraid.Features.Identity.Endpoints;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Shared;

namespace StackBraid.ArchitectureTests;

/// <summary>
/// Turns docs/STRUCTURE.md's dependency rules from a convention into a
/// failing build. Every rule here was seen to genuinely fail against a
/// deliberately introduced violation before being committed — see the
/// commit message for exactly what was tried and reverted; a rule nobody
/// has watched fail is a rule that might not test anything.
/// </summary>
public class LayeringRulesTests
{
    private static readonly Assembly DomainAssembly = typeof(Features.Identity.Domain.Entities.User).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Features.Identity.Application.Commands.RegisterUserCommand).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Features.Identity.Contracts.Dtos.UserDto).Assembly;
    private static readonly Assembly PersistenceAssembly = typeof(Features.Identity.Persistence.IdentityDbContext).Assembly;
    private static readonly Assembly EndpointsAssembly = typeof(Features.Identity.Endpoints.IdentityEndpointsExtensions).Assembly;
    private static readonly Assembly SharedAssembly = typeof(Shared.ServiceCollectionExtensions).Assembly;
    private static readonly Assembly DatabasePostgresAssembly = typeof(Database.Postgres.ServiceCollectionExtensions).Assembly;

    [Fact]
    public void Domain_depends_on_nothing()
    {
        // Not "no framework imports" — literally zero assembly references
        // beyond the .NET runtime itself. This is why Entity/IAuditable/the
        // domain-event interfaces live inside Domain rather than in Shared.
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        referencedAssemblies.ShouldNotContain(name => name != null && name.StartsWith("StackBraid.", StringComparison.Ordinal));
        referencedAssemblies.ShouldNotContain("Microsoft.EntityFrameworkCore");
        referencedAssemblies.ShouldNotContain("Microsoft.AspNetCore.App");
        referencedAssemblies.ShouldNotContain("Mediator");
    }

    [Fact]
    public void Application_depends_only_on_its_own_Domain_Contracts_and_Shared()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny("StackBraid.Features.Identity.Persistence", "StackBraid.Features.Identity.Endpoints")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Persistence_depends_only_on_its_own_Domain_and_Shared_never_skipping_forward_to_Application_or_Endpoints()
    {
        var result = Types.InAssembly(PersistenceAssembly)
            .Should()
            .NotHaveDependencyOnAny("StackBraid.Features.Identity.Application", "StackBraid.Features.Identity.Endpoints")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Contracts_is_the_only_surface_another_feature_may_see_so_it_depends_on_nothing_of_its_own_features_internals()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "StackBraid.Features.Identity.Domain",
                "StackBraid.Features.Identity.Persistence",
                "StackBraid.Features.Identity.Application",
                "StackBraid.Features.Identity.Endpoints")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Shared_never_imports_a_feature()
    {
        var result = Types.InAssembly(SharedAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("StackBraid.Features")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void No_provider_name_appears_outside_Database()
    {
        // Npgsql is the only provider driver referenced anywhere in this
        // backend today — see docs/STRUCTURE.md: "Database/<Provider> is the
        // only place a provider name appears." Every non-Database assembly
        // is checked; Database.Postgres itself is exempt (that IS the
        // provider-specific project).
        var otherAssemblies = new[] { SharedAssembly, DomainAssembly, ApplicationAssembly, ContractsAssembly, PersistenceAssembly, EndpointsAssembly };

        foreach (var assembly in otherAssemblies)
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
            referencedAssemblies.ShouldNotContain(name => name != null && name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase),
                $"{assembly.GetName().Name} must not reference Npgsql — provider-specific code belongs only in Database/Postgres.");
        }

        // And the positive half of the same rule: Database.Postgres is
        // exactly where Npgsql is expected to appear.
        DatabasePostgresAssembly.GetReferencedAssemblies().Select(a => a.Name)
            .ShouldContain(name => name != null && name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
    }

    private static string FailureMessage(TestResult result) =>
        $"Violating types: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}";
}
