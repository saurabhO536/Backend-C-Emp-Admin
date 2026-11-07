namespace EmployeeMng.API.Models;

/// <summary>Employee totals for a department represented in the workforce.</summary>
public sealed record DepartmentSummaryResponse(
    string Name,
    int EmployeeCount,
    int ActiveEmployeeCount);