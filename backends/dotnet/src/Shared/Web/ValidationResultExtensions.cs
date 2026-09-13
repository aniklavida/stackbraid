using FluentValidation.Results;

namespace StackBraid.Shared.Web;

/// <summary>
/// Turns a FluentValidation outcome into the same <see cref="AppError"/>
/// shape every other failure uses, so a command handler's validation
/// failure reaches the client through the identical Problem envelope as a
/// not-found or a conflict — one failure path, not two.
/// </summary>
public static class ValidationResultExtensions
{
    public static AppError ToAppError(this ValidationResult result, string code, string messageKey)
    {
        var fieldErrors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return AppError.Validation(code, messageKey, fieldErrors);
    }
}
