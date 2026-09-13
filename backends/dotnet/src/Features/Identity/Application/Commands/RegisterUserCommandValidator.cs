using FluentValidation;

namespace StackBraid.Features.Identity.Application.Commands;

/// <summary>
/// Mirrors <c>contract/openapi.yaml</c>'s <c>RegisterRequest</c> constraints
/// exactly (email format, 8-character password minimum, a 1–200 character
/// display name). Property names are overridden to the request body's own
/// camelCase field names, since <c>Result.Error.FieldErrors</c> keys become
/// <c>Problem.errors</c> keys a client matches against its form fields.
/// </summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("validation.email.required").OverridePropertyName("email")
            .EmailAddress().WithMessage("validation.email.invalid").OverridePropertyName("email");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("validation.password.required").OverridePropertyName("password")
            .MinimumLength(8).WithMessage("validation.password.min_length").OverridePropertyName("password");

        RuleFor(c => c.DisplayName)
            .NotEmpty().WithMessage("validation.display_name.required").OverridePropertyName("displayName")
            .Length(1, 200).WithMessage("validation.display_name.length").OverridePropertyName("displayName");
    }
}
