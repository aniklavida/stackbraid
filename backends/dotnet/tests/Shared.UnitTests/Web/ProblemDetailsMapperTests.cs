using Shouldly;
using StackBraid.Shared.Localization;
using StackBraid.Shared.Web;

namespace StackBraid.Shared.UnitTests.Web;

public class ProblemDetailsMapperTests
{
    private readonly JsonAppLocalizer _localizer = new();

    [Fact]
    public void Map_sets_the_rfc9457_core_fields()
    {
        var error = AppError.Unauthorized("IDENTITY.INVALID_CREDENTIALS", "identity.invalid_credentials");

        var problem = ProblemDetailsMapper.Map(error, _localizer, "en", "trace-1", "/v1/auth/login");

        problem.Type.ShouldBe("about:blank");
        problem.Status.ShouldBe(401);
        problem.Title.ShouldBe("Unauthorized");
        problem.Detail.ShouldBe("Invalid email or password.");
        problem.Instance.ShouldBe("/v1/auth/login");
    }

    [Fact]
    public void Map_carries_the_stable_code_and_the_trace_id_as_extensions()
    {
        var error = AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found");

        var problem = ProblemDetailsMapper.Map(error, _localizer, "en", "trace-42", "/v1/users/x");

        problem.Extensions["code"].ShouldBe("IDENTITY.USER_NOT_FOUND");
        problem.Extensions["traceId"].ShouldBe("trace-42");
    }

    [Fact]
    public void Map_localizes_title_and_detail_but_never_the_code()
    {
        var error = AppError.Unauthorized("IDENTITY.INVALID_CREDENTIALS", "identity.invalid_credentials");

        var problem = ProblemDetailsMapper.Map(error, _localizer, "es", "trace-1", "/v1/auth/login");

        problem.Title.ShouldBe("No autorizado");
        problem.Detail.ShouldBe("Correo electrónico o contraseña no válidos.");
        problem.Extensions["code"].ShouldBe("IDENTITY.INVALID_CREDENTIALS");
    }

    [Fact]
    public void Map_populates_errors_only_for_validation_failures()
    {
        var fieldErrors = new Dictionary<string, string[]> { ["email"] = ["validation.email.invalid"] };
        var error = AppError.Validation("IDENTITY.VALIDATION_FAILED", "identity.validation_failed", fieldErrors);

        var problem = ProblemDetailsMapper.Map(error, _localizer, "en", "trace-1", "/v1/auth/register");

        problem.Status.ShouldBe(400);
        var errors = problem.Extensions["errors"].ShouldBeOfType<Dictionary<string, string[]>>();
        errors["email"].ShouldBe(["must be a valid email address"]);
    }

    [Fact]
    public void Map_omits_errors_for_a_non_validation_failure()
    {
        var error = AppError.Conflict("IDENTITY.EMAIL_ALREADY_REGISTERED", "identity.email_already_registered");

        var problem = ProblemDetailsMapper.Map(error, _localizer, "en", "trace-1", "/v1/auth/register");

        problem.Extensions.ShouldNotContainKey("errors");
    }
}
