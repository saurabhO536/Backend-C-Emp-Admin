using EmployeeMng.API.Models;

namespace EmployeeMng.API.Services;

public interface IAttendanceService
{
    Task<AttendanceListResponse> GetListAsync(
        AttendanceListQuery query,
        CancellationToken cancellationToken);

    Task<AttendanceResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<AttendanceSummaryResponse> GetSummaryAsync(
        DateOnly? date,
        CancellationToken cancellationToken);

    Task<AttendanceOperationResult<AttendanceResponse>> CreateAsync(
        CreateAttendanceRequest request,
        CancellationToken cancellationToken);

    Task<AttendanceOperationResult<AttendanceResponse>> UpdateAsync(
        int id,
        UpdateAttendanceRequest request,
        CancellationToken cancellationToken);

    Task<AttendanceOperationResult<AttendanceResponse>> CheckOutAsync(
        int id,
        CancellationToken cancellationToken);

    Task<AttendanceOperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken);
}
