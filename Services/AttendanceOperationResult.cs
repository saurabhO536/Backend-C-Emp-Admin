namespace EmployeeMng.API.Services;

public enum AttendanceErrorCode
{
    Validation,
    NotFound,
    EmployeeNotFound,
    Conflict
}

public sealed record AttendanceOperationResult<T>(
    T? Value,
    AttendanceErrorCode? ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorCode is null;

    public static AttendanceOperationResult<T> Success(T value) =>
        new(value, null, null);

    public static AttendanceOperationResult<T> Failure(
        AttendanceErrorCode errorCode,
        string errorMessage) =>
        new(default, errorCode, errorMessage);
}
