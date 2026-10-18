namespace EmployeeMng.API.Models;

public class Attendance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public DateOnly AttendanceDate { get; set; }

    public DateTimeOffset? CheckInTime { get; set; }

    public DateTimeOffset? CheckOutTime { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
