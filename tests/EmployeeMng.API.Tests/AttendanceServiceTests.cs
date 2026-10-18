using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using EmployeeMng.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeMng.API.Tests;

public sealed class AttendanceServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsDuplicateEmployeeAndDate()
    {
        await using var context = CreateContext();
        context.Employees.Add(CreateEmployee(1));
        await context.SaveChangesAsync();
        var service = new AttendanceService(context);
        var request = CreateRequest(1, DateOnly.FromDateTime(DateTime.UtcNow));

        var first = await service.CreateAsync(request, CancellationToken.None);
        var second = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.Equal(AttendanceErrorCode.Conflict, second.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingEmployee()
    {
        await using var context = CreateContext();
        var service = new AttendanceService(context);

        var result = await service.CreateAsync(
            CreateRequest(123, DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.Equal(AttendanceErrorCode.EmployeeNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_RejectsAbsentWithCheckIn()
    {
        await using var context = CreateContext();
        context.Employees.Add(CreateEmployee(1));
        await context.SaveChangesAsync();
        var service = new AttendanceService(context);
        var now = DateTimeOffset.UtcNow;

        var result = await service.CreateAsync(
            CreateRequest(
                1,
                DateOnly.FromDateTime(now.UtcDateTime),
                AttendanceStatus.Absent,
                checkInTime: now),
            CancellationToken.None);

        Assert.Equal(AttendanceErrorCode.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_RejectsCheckOutWithoutCheckIn()
    {
        await using var context = CreateContext();
        context.Employees.Add(CreateEmployee(1));
        await context.SaveChangesAsync();
        var service = new AttendanceService(context);
        var now = DateTimeOffset.UtcNow;

        var result = await service.CreateAsync(
            new CreateAttendanceRequest
            {
                EmployeeId = 1,
                AttendanceDate = DateOnly.FromDateTime(now.UtcDateTime),
                Status = AttendanceStatus.Present,
                CheckOutTime = now
            },
            CancellationToken.None);

        Assert.Equal(AttendanceErrorCode.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task GetListAsync_FiltersAndPaginatesWithEmployeeDetails()
    {
        await using var context = CreateContext();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var employees = new[] { CreateEmployee(1), CreateEmployee(2) };
        context.Employees.AddRange(employees);
        context.Attendances.AddRange(
            CreateRecord(employees[0], date, AttendanceStatus.Present),
            CreateRecord(employees[1], date, AttendanceStatus.Present));
        await context.SaveChangesAsync();
        var service = new AttendanceService(context);

        var result = await service.GetListAsync(
            new AttendanceListQuery
            {
                Date = date,
                Department = "Operations",
                Status = AttendanceStatus.Present,
                Page = 1,
                PageSize = 1
            },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("First1", result.Items[0].Employee.FirstName);
        Assert.Equal(1, result.Items[0].EmployeeId);
    }

    [Fact]
    public async Task GetSummaryAsync_ExcludesInactiveEmployeesAndCountsUnmarked()
    {
        await using var context = CreateContext();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var employees = Enumerable.Range(1, 6)
            .Select(id => CreateEmployee(id))
            .Append(CreateEmployee(7, isActive: false))
            .ToList();
        context.Employees.AddRange(employees);
        context.Attendances.AddRange(
            CreateRecord(employees[0], date, AttendanceStatus.Present),
            CreateRecord(employees[1], date, AttendanceStatus.Absent),
            CreateRecord(employees[2], date, AttendanceStatus.Late),
            CreateRecord(employees[3], date, AttendanceStatus.HalfDay),
            CreateRecord(employees[4], date, AttendanceStatus.OnLeave),
            CreateRecord(employees[6], date, AttendanceStatus.Present));
        await context.SaveChangesAsync();
        var service = new AttendanceService(context);

        var summary = await service.GetSummaryAsync(date, CancellationToken.None);

        Assert.Equal(6, summary.TotalEmployees);
        Assert.Equal(1, summary.Present);
        Assert.Equal(1, summary.Absent);
        Assert.Equal(1, summary.Late);
        Assert.Equal(1, summary.HalfDay);
        Assert.Equal(1, summary.OnLeave);
        Assert.Equal(1, summary.NotYetMarked);
    }

    [Fact]
    public async Task CheckOutAsync_UsesServerTimeAndRejectsSecondCheckout()
    {
        await using var context = CreateContext();
        var employee = CreateEmployee(1);
        var now = DateTimeOffset.UtcNow;
        var record = CreateRecord(
            employee,
            DateOnly.FromDateTime(now.UtcDateTime),
            AttendanceStatus.Present,
            now.AddMinutes(-30));
        context.Attendances.Add(record);
        await context.SaveChangesAsync();
        var service = new AttendanceService(context);

        var first = await service.CheckOutAsync(record.Id, CancellationToken.None);
        var second = await service.CheckOutAsync(record.Id, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.NotNull(first.Value?.CheckOutTime);
        Assert.Equal(AttendanceErrorCode.Conflict, second.ErrorCode);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Employee CreateEmployee(int id, bool isActive = true) =>
        new()
        {
            Id = id,
            FirstName = $"First{id}",
            LastName = $"Last{id}",
            Email = $"employee{id}@example.test",
            Department = "Operations",
            Salary = 0,
            CreatedDate = DateTime.UtcNow,
            IsActive = isActive
        };

    private static CreateAttendanceRequest CreateRequest(
        int employeeId,
        DateOnly date,
        AttendanceStatus status = AttendanceStatus.Present,
        DateTimeOffset? checkInTime = null) =>
        new()
        {
            EmployeeId = employeeId,
            AttendanceDate = date,
            Status = status,
            CheckInTime = checkInTime
        };

    private static Attendance CreateRecord(
        Employee employee,
        DateOnly date,
        AttendanceStatus status,
        DateTimeOffset? checkInTime = null) =>
        new()
        {
            Employee = employee,
            EmployeeId = employee.Id,
            AttendanceDate = date,
            Status = status,
            CheckInTime = checkInTime,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
