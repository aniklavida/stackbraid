using Shouldly;
using StackBraid.Shared.Web;

namespace StackBraid.Shared.UnitTests.Web;

public class ResultTests
{
    [Fact]
    public void Success_carries_the_value()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        var error = AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found");

        var result = Result<int>.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void An_AppError_implicitly_converts_to_a_failed_result()
    {
        var error = AppError.Forbidden("IDENTITY.FORBIDDEN", "identity.forbidden");

        Result<string> result = error;

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Match_invokes_the_success_branch_only_on_success()
    {
        var result = Result<int>.Success(7);

        var text = result.Match(v => $"ok:{v}", e => $"err:{e.Code}");

        text.ShouldBe("ok:7");
    }

    [Fact]
    public void Match_invokes_the_failure_branch_only_on_failure()
    {
        var result = Result<int>.Failure(AppError.NotFound("X", "y"));

        var text = result.Match(v => $"ok:{v}", e => $"err:{e.Code}");

        text.ShouldBe("err:X");
    }
}
