using System.ComponentModel.DataAnnotations;

namespace EmployeeMng.API.Models;

/// <summary>Fields required to submit a leave request.</summary>
public sealed record CreateLeaveRequest
{
    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    [Required, MaxLength(100)]
    public required string LeaveType { get; init; }

    [Required, MaxLength(2000)]
    public required string Reason { get; init; }
}

/// <summary>An administrator's decision and optional note for a leave request.</summary>
public sealed record ReviewLeaveRequest
{
    [Required]
    [EnumDataType(typeof(LeaveRequestStatus))]
    public required LeaveRequestStatus? Decision { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

/// <summary>Leave request data returned by the API.</summary>
public sealed record LeaveRequestResponse(
    int Id,
    int EmployeeId,
    string EmployeeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string LeaveType,
    string Reason,
    LeaveRequestStatus Status,
    DateTimeOffset CreatedAt,
    int? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? ReviewNotes);

/// <summary>Associates a user account with an employee record.</summary>
public sealed record LinkEmployeeRequest
{
    [Range(1, int.MaxValue)]
    public required int EmployeeId { get; init; }
}