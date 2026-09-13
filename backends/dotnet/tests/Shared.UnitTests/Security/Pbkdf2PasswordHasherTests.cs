using Shouldly;
using StackBraid.Shared.Security;

namespace StackBraid.Shared.UnitTests.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Verify_accepts_the_correct_password()
    {
        var hash = _sut.Hash("correct horse battery staple");

        _sut.Verify("correct horse battery staple", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_rejects_an_incorrect_password()
    {
        var hash = _sut.Hash("correct horse battery staple");

        _sut.Verify("wrong password", hash).ShouldBeFalse();
    }

    [Fact]
    public void Hash_salts_so_the_same_password_hashes_differently_each_time()
    {
        var first = _sut.Hash("same password");
        var second = _sut.Hash("same password");

        first.ShouldNotBe(second);
        _sut.Verify("same password", first).ShouldBeTrue();
        _sut.Verify("same password", second).ShouldBeTrue();
    }

    [Fact]
    public void Verify_rejects_a_malformed_hash_instead_of_throwing()
    {
        _sut.Verify("anything", "not-a-real-hash").ShouldBeFalse();
    }

    [Fact]
    public void Hash_encodes_the_algorithm_and_iteration_count_so_a_future_increase_stays_verifiable()
    {
        var hash = _sut.Hash("password");

        hash.ShouldStartWith("pbkdf2-sha256$600000$");
    }
}
