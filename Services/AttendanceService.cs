using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EmployeeMng.API.Services;

public sealed class AttendanceService(ApplicationDbContext context)
    : IAttendanceService
{
    private const string DailyAttendanceIndexName =
        "IX_Attendance_EmployeeId_AttendanceDate";

    public async Task<AttendanceListResponse> GetListAsync(
        AttendanceListQuery query,
        CancellationToken cancellationToken)
    {
        var attendance = context.Attendances
            .AsNoTracking()
            .AsQueryable();

        if (query.Date is { } date)
        {
            attendance = attendance.Where(record => record.AttendanceDate == date);
        }

        if (query.StartDate is { } startDate)
        {
            attendance = attendance.Where(record => record.AttendanceDate >= startDate);
        }

        if (query.EndDate is { } endDate)
        {
            attendance = attendance.Where(record => record.AttendanceDate <= endDate);
        }

        if (query.EmployeeId is { } employeeId)
        {
            attendance = attendance.Where(record => record.EmployeeId == employeeId);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            attendance = attendance.Where(record =>
                record.Employee.Department == query.Department);
        }

        if (query.Status is { } status)
        {
            attendance = attendance.Where(record => record.Status == status);
        }

        var totalCount = await attendance.CountAsync(cancellationToken);
        var records = await attendance
            .OrderBy(record => record.AttendanceDate)
            .ThenBy(record => record.EmployeeId)
            .ThenBy(record => record.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => new AttendanceResponse(
                record.Id,
                record.EmployeeId,
                record.AttendanceDate,
                record.CheckInTime,
                record.CheckOutTime,
                record.Status,
                record.Notes,
                record.CreatedAt,
                record.UpdatedAt,
                new AttendanceEmployeeResponse(
                    record.Employee.Id,
                    record.Employee.FirstName,
                    record.Employee.LastName,
                    record.Employee.Department)))
            .ToListAsync(cancellationToken);

        return new AttendanceListResponse(
            records,
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<AttendanceResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await context.Attendances
            .AsNoTracking()
            .Where(attendance => attendance.Id == id)
            .Select(attendance => new AttendanceResponse(
                attendance.Id,
                attendance.EmployeeId,
                attendance.AttendanceDate,
                attendance.CheckInTime,
                attendance.CheckOutTime,
                attendance.Status,
                attendance.Notes,
                attendance.CreatedAt,
                attendance.UpdatedAt,
                new AttendanceEmployeeResponse(
                    attendance.Employee.Id,
                    attendance.Employee.FirstName,
                    attendance.Employee.LastName,
                    attendance.Employee.Department)))
            .SingleOrDefaultAsync(cancellationToken);

        return record;
    }

    public async Task<AttendanceSummaryResponse> GetSummaryAsync(
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var summaryDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var totalEmployees = await context.Employees
            .CountAsync(employee => employee.IsActive, cancellationToken);
        var counts = await context.Attendances
            .Where(record =>
                record.AttendanceDate == summaryDate &&
                record.Employee.IsActive)
            .GroupBy(record => record.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        var present = GetCount(counts, AttendanceStatus.Present);
        var absent = GetCount(counts, AttendanceStatus.Absent);
        var late = GetCount(counts, AttendanceStatus.Late);
        var halfDay = GetCount(counts, AttendanceStatus.HalfDay);
        var onLeave = GetCount(counts, AttendanceStatus.OnLeave);
        var markedCount = present + absent + late + halfDay + onLeave;

        return new AttendanceSummaryResponse(
            summaryDate,
            totalEmployees,
            present,
            absent,
            late,
            halfDay,
            onLeave,
            Math.Max(0, totalEmployees - markedCount));
    }

    public async Task<AttendanceOperationResult<AttendanceResponse>> CreateAsync(
        CreateAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRecord(
            request.EmployeeId,
            request.AttendanceDate,
            request.Status,
            request.CheckInTime,
            request.CheckOutTime);
        if (validationError is not null)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Validation,
                validationError);
        }

        var employee = await context.Employees
            .SingleOrDefaultAsync(
                employee => employee.Id == request.EmployeeId,
                cancellationToken);
        if (employee is null)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.EmployeeNotFound,
                "The specified employee does not exist.");
        }

        var duplicateExists = await context.Attendances.AnyAsync(record =>
                record.EmployeeId == request.EmployeeId &&
                record.AttendanceDate == request.AttendanceDate,
            cancellationToken);
        if (duplicateExists)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "Attendance has already been recorded for this employee and date.");
        }

        var now = DateTimeOffset.UtcNow;
        var record = new Attendance
        {
            EmployeeId = request.EmployeeId,
            Employee = employee,
            AttendanceDate = request.AttendanceDate,
            CheckInTime = ToUtc(request.CheckInTime),
            CheckOutTime = ToUtc(request.CheckOutTime),
            Status = request.Status!.Value,
            Notes = request.Notes,
            CreatedAt = now
        };

        context.Attendances.Add(record);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDailyAttendanceViolation(exception))
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "Attendance has already been recorded for this employee and date.");
        }

        return AttendanceOperationResult<AttendanceResponse>.Success(ToResponse(record));
    }

    public async Task<AttendanceOperationResult<AttendanceResponse>> UpdateAsync(
        int id,
        UpdateAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var record = await context.Attendances
            .Include(attendance => attendance.Employee)
            .SingleOrDefaultAsync(attendance => attendance.Id == id, cancellationToken);
        if (record is null)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.NotFound,
                "Attendance record was not found.");
        }

        var validationError = ValidateRecord(
            request.EmployeeId,
            request.AttendanceDate,
            request.Status,
            request.CheckInTime,
            request.CheckOutTime);
        if (validationError is not null)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Validation,
                validationError);
        }

        var employee = await context.Employees
            .SingleOrDefaultAsync(
                employee => employee.Id == request.EmployeeId,
                cancellationToken);
        if (employee is null)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.EmployeeNotFound,
                "The specified employee does not exist.");
        }

        var duplicateExists = await context.Attendances.AnyAsync(other =>
                other.Id != id &&
                other.EmployeeId == request.EmployeeId &&
                other.AttendanceDate == request.AttendanceDate,
            cancellationToken);
        if (duplicateExists)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "Attendance has already been recorded for this employee and date.");
        }

        record.EmployeeId = request.EmployeeId;
        record.Employee = employee;
        record.AttendanceDate = request.AttendanceDate;
        record.CheckInTime = ToUtc(request.CheckInTime);
        record.CheckOutTime = ToUtc(request.CheckOutTime);
        record.Status = request.Status!.Value;
        record.Notes = request.Notes;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "The attendance record was changed by another request.");
        }
        catch (DbUpdateException exception) when (IsDailyAttendanceViolation(exception))
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "Attendance has already been recorded for this employee and date.");
        }

        return AttendanceOperationResult<AttendanceResponse>.Success(ToResponse(record));
    }

    public async Task<AttendanceOperationResult<AttendanceResponse>> CheckOutAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await context.Attendances
            .Include(attendance => attendance.Employee)
            .SingleOrDefaultAsync(attendance => attendance.Id == id, cancellationToken);
        if (record is null)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.NotFound,
                "Attendance record was not found.");
        }

        if (record.CheckOutTime is not null)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "A check-out has already been recorded.");
        }

        if (record.CheckInTime is null ||
            record.Status is AttendanceStatus.Absent or AttendanceStatus.OnLeave)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "This attendance record is not open for check-out.");
        }

        var checkOutTime = DateTimeOffset.UtcNow;
        if (DateOnly.FromDateTime(checkOutTime.UtcDateTime) != record.AttendanceDate)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "Check-out is only available on the attendance date.");
        }

        if (checkOutTime < record.CheckInTime)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Validation,
                "Check-out cannot be earlier than check-in.");
        }

        record.CheckOutTime = checkOutTime;
        record.UpdatedAt = checkOutTime;
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure<AttendanceResponse>(
                AttendanceErrorCode.Conflict,
                "The attendance record was checked out by another request.");
        }

        return AttendanceOperationResult<AttendanceResponse>.Success(ToResponse(record));
    }

    public async Task<AttendanceOperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await context.Attendances
            .SingleOrDefaultAsync(attendance => attendance.Id == id, cancellationToken);
        if (record is null)
        {
            return Failure<bool>(
                AttendanceErrorCode.NotFound,
                "Attendance record was not found.");
        }

        context.Attendances.Remove(record);
        await context.SaveChangesAsync(cancellationToken);
        return AttendanceOperationResult<bool>.Success(true);
    }

    private static string? ValidateRecord(
        int employeeId,
        DateOnly attendanceDate,
        AttendanceStatus? status,
        DateTimeOffset? checkInTime,
        DateTimeOffset? checkOutTime)
    {
        if (employeeId <= 0)
        {
            return "EmployeeId must be a positive integer.";
        }

        if (attendanceDate == DateOnly.MinValue)
        {
            return "AttendanceDate is required.";
        }

        if (status is null || !Enum.IsDefined(status.Value))
        {
            return "Status must be Present, Absent, Late, HalfDay, or OnLeave.";
        }

        if (checkInTime is { } checkIn &&
            DateOnly.FromDateTime(checkIn.UtcDateTime) != attendanceDate)
        {
            return "Check-in must fall on the attendance date in UTC.";
        }

        if (checkOutTime is { } checkOut &&
            DateOnly.FromDateTime(checkOut.UtcDateTime) != attendanceDate)
        {
            return "Check-out must fall on the attendance date in UTC.";
        }

        if (checkInTime > DateTimeOffset.UtcNow || checkOutTime > DateTimeOffset.UtcNow)
        {
            return "Attendance timestamps cannot be in the future.";
        }

        if (checkOutTime is not null && checkInTime is null)
        {
            return "A check-in time is required when a check-out time is provided.";
        }

        if (checkInTime is { } checkedIn && checkOutTime is { } checkedOut &&
            checkedOut < checkedIn)
        {
            return "Check-out cannot be earlier than check-in.";
        }

        if (status is AttendanceStatus.Late && checkInTime is null)
        {
            return "Late attendance requires a check-in time.";
        }

        if (status is AttendanceStatus.Absent or AttendanceStatus.OnLeave &&
            (checkInTime is not null || checkOutTime is not null))
        {
            return "Absent and OnLeave records cannot have check-in or check-out times.";
        }

        return null;
    }

    private static DateTimeOffset? ToUtc(DateTimeOffset? value) =>
        value?.ToUniversalTime();

    private static int GetCount(
        IReadOnlyDictionary<AttendanceStatus, int> counts,
        AttendanceStatus status) =>
        counts.TryGetValue(status, out var count) ? count : 0;

    private static bool IsDailyAttendanceViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DailyAttendanceIndexName
        };

    private static AttendanceResponse ToResponse(Attendance record) =>
        new(
            record.Id,
            record.EmployeeId,
            record.AttendanceDate,
            record.CheckInTime,
            record.CheckOutTime,
            record.Status,
            record.Notes,
            record.CreatedAt,
            record.UpdatedAt,
            new AttendanceEmployeeResponse(
                record.Employee.Id,
                record.Employee.FirstName,
                record.Employee.LastName,
                record.Employee.Department));

    private static AttendanceOperationResult<T> Failure<T>(
        AttendanceErrorCode errorCode,
        string message) =>
        AttendanceOperationResult<T>.Failure(errorCode, message);
}
