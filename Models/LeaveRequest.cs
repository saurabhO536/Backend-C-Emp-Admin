namespace EmployeeMng.API.Models;

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class LeaveRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string LeaveType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public LeaveRequestStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewNotes { get; set; }
}