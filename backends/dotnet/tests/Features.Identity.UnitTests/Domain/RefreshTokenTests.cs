using Shouldly;
using StackBraid.Features.Identity.Domain.Entities;

namespace StackBraid.Features.Identity.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsActive_is_true_for_a_freshly_issued_token()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(7), Now);

        token.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_is_false_once_the_token_expires()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddMinutes(1), Now);

        token.IsActive(Now.AddMinutes(2)).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_is_false_once_revoked()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(7), Now);

        token.Revoke(Now);

        token.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_records_the_replacement_token_for_rotation()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(7), Now);
        var replacementId = Guid.NewGuid();

        token.Revoke(Now, replacementId);

        token.ReplacedByTokenId.ShouldBe(replacementId);
        token.RevokedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Revoke_is_idempotent_it_does_not_overwrite_the_first_revocation()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(7), Now);
        var firstRevocationTime = Now.AddMinutes(5);

        token.Revoke(firstRevocationTime);
        token.Revoke(Now.AddMinutes(10));

        token.RevokedAtUtc.ShouldBe(firstRevocationTime);
    }
}
