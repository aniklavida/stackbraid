using Shouldly;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.UnitTests.Domain;

public class EmailTests
{
    [Fact]
    public void Create_normalizes_to_lowercase()
    {
        Email.Create("Ada@Example.COM").Value.ShouldBe("ada@example.com");
    }

    [Fact]
    public void Create_trims_surrounding_whitespace()
    {
        Email.Create("  ada@example.com  ").Value.ShouldBe("ada@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    public void Create_rejects_an_invalid_address(string input)
    {
        Should.Throw<ArgumentException>(() => Email.Create(input));
    }

    [Fact]
    public void Two_emails_with_different_casing_are_equal()
    {
        Email.Create("Ada@Example.com").ShouldBe(Email.Create("ada@example.com"));
    }
}
