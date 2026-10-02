namespace doanki3API.Services.Common;

public enum ServiceErrorType
{
    Validation,
    NotFound,
    Conflict
}

public sealed record ServiceResult<T>(
    T? Value,
    string? Error,
    ServiceErrorType? ErrorType)
{
    public bool IsSuccess => ErrorType is null;

    public static ServiceResult<T> Success(T value) => new(value, null, null);

    public static ServiceResult<T> Validation(string error) =>
        new(default, error, ServiceErrorType.Validation);

    public static ServiceResult<T> NotFound(string error) =>
        new(default, error, ServiceErrorType.NotFound);

    public static ServiceResult<T> Conflict(string error) =>
        new(default, error, ServiceErrorType.Conflict);
}
