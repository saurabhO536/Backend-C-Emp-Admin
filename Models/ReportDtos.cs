using EmployeeMng.API.Models;

namespace EmployeeMng.API.Models;

/// <summary>Current employee totals grouped by department.</summary>
public sealed record WorkforceReportResponse(
    int TotalEmployees,
    int ActiveEmployees,
    IReadOnlyList<DepartmentSummaryResponse> Departments);

/// <summary>Attendance status totals for one date.</summary>
public sealed record DailyAttendanceReportResponse(
    DateOnly Date,
    int Present,
    int Absent,
    int Late,
    int HalfDay,
    int OnLeave,
    int TotalMarked);