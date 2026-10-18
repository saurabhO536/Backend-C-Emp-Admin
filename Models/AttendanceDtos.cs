using System.ComponentModel.DataAnnotations;

namespace EmployeeMng.API.Models;

/// <summary>Query filters and paging options for attendance records.</summary>
public sealed record AttendanceListQuery
{
    public DateOnly? Date { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 200)]
    public int PageSize { get; init; } = 50;

    [Range(1, int.MaxValue)]
    public int? EmployeeId { get; init; }

    [MaxLength(200)]
    public string? Department { get; init; }

    [EnumDataType(typeof(AttendanceStatus))]
    public AttendanceStatus? Status { get; init; }
}

/// <summary>Fields required to create an attendance record.</summary>
public sealed record CreateAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public required int EmployeeId { get; init; }

    public required DateOnly AttendanceDate { get; init; }

    [Required]
    public required AttendanceStatus? Status { get; init; }

    public DateTimeOffset? CheckInTime { get; init; }

    public DateTimeOffset? CheckOutTime { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

/// <summary>Fields that replace an existing attendance record.</summary>
public sealed record UpdateAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public required int EmployeeId { get; init; }

    public required DateOnly AttendanceDate { get; init; }

    [Required]
    public required AttendanceStatus? Status { get; init; }

    public DateTimeOffset? CheckInTime { get; init; }

    public DateTimeOffset? CheckOutTime { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

/// <summary>Employee details included with an attendance record.</summary>
public sealed record AttendanceEmployeeResponse(
    int Id,
    string FirstName,
    string LastName,
    string Department);

/// <summary>Attendance record returned by the API.</summary>
public sealed record AttendanceResponse(
    int Id,
    int EmployeeId,
    DateOnly AttendanceDate,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    AttendanceStatus Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    AttendanceEmployeeResponse Employee);

/// <summary>A page of attendance records.</summary>
public sealed record AttendanceListResponse(
    IReadOnlyList<AttendanceResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Attendance counts for active employees on one date.</summary>
public sealed record AttendanceSummaryResponse(
    DateOnly Date,
    int TotalEmployees,
    int Present,
    int Absent,
    int Late,
    int HalfDay,
    int OnLeave,
    int NotYetMarked);
