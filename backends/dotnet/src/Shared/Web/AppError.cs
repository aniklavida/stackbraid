namespace StackBraid.Shared.Web;

/// <summary>
/// The failure shape every handler returns instead of throwing for an
/// expected, nameable condition ("email already registered", "user not
/// found"). <see cref="Code"/> is the stable string the contract's
/// <c>Problem.code</c> field carries — see <c>contract/openapi.yaml</c>.
/// <see cref="MessageKey"/> is a localization resource key; the handler
/// never renders human text itself, so the same error reads correctly in
/// every configured locale.
/// </summary>
public sealed record AppError(string Code, AppErrorType Type, string MessageKey, IReadOnlyDictionary<string, string>? Arguments = null)
{
    public static AppError NotFound(string code, string messageKey) => new(code, AppErrorType.NotFound, messageKey);

    public static AppError Conflict(string code, string messageKey) => new(code, AppErrorType.Conflict, messageKey);

    public static AppError Unauthorized(string code, string messageKey) => new(code, AppErrorType.Unauthorized, messageKey);

    public static AppError Forbidden(string code, string messageKey) => new(code, AppErrorType.Forbidden, messageKey);

    public static AppError Validation(string code, string messageKey, IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new(code, AppErrorType.Validation, messageKey) { FieldErrors = fieldErrors };

    /// <summary>Populated only for <see cref="AppErrorType.Validation"/> — maps a field name to its violation message keys.</summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; private init; }
}

public enum AppErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure,
}
