using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/reports")]
public sealed class ReportsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("workforce")]
    public async Task<ActionResult<WorkforceReportResponse>> GetWorkforceReport(
        CancellationToken cancellationToken)
    {
        var totals = await context.Employees
            .AsNoTracking()
            .GroupBy(employee => employee.Department)
            .Select(group => new
            {
                Name = group.Key,
                EmployeeCount = group.Count(),
                ActiveEmployeeCount = group.Count(employee => employee.IsActive)
            })
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);

        var activeEmployees = totals.Sum(department => department.ActiveEmployeeCount);
        var report = new WorkforceReportResponse(
            totals.Sum(department => department.EmployeeCount),
            activeEmployees,
            totals.Select(department => new DepartmentSummaryResponse(
                department.Name,
                department.EmployeeCount,
                department.ActiveEmployeeCount)).ToList());

        return Ok(report);
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<IReadOnlyList<DailyAttendanceReportResponse>>> GetAttendanceReport(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid date range",
                detail: "StartDate must be on or before EndDate.");
        }

        if (endDate.DayNumber - startDate.DayNumber > 365)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Date range too large",
                detail: "The attendance report is limited to 366 days.");
        }

        var groupedCounts = await context.Attendances
            .AsNoTracking()
            .Where(record => record.AttendanceDate >= startDate && record.AttendanceDate <= endDate)
            .GroupBy(record => new { record.AttendanceDate, record.Status })
            .Select(group => new
            {
                group.Key.AttendanceDate,
                group.Key.Status,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var countsByDate = groupedCounts
            .GroupBy(item => item.AttendanceDate)
            .ToDictionary(group => group.Key, group => group.ToDictionary(item => item.Status, item => item.Count));

        var report = new List<DailyAttendanceReportResponse>();
        var dayCount = endDate.DayNumber - startDate.DayNumber;
        for (var dayOffset = 0; dayOffset <= dayCount; dayOffset++)
        {
            var date = DateOnly.FromDayNumber(startDate.DayNumber + dayOffset);
            countsByDate.TryGetValue(date, out var counts);
            var present = Count(AttendanceStatus.Present);
            var absent = Count(AttendanceStatus.Absent);
            var late = Count(AttendanceStatus.Late);
            var halfDay = Count(AttendanceStatus.HalfDay);
            var onLeave = Count(AttendanceStatus.OnLeave);

            report.Add(new DailyAttendanceReportResponse(
                date,
                present,
                absent,
                late,
                halfDay,
                onLeave,
                present + absent + late + halfDay + onLeave));

            int Count(AttendanceStatus status) => counts?.GetValueOrDefault(status) ?? 0;
        }

        return Ok(report);
    }
}