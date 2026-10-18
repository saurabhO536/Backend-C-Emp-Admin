using EmployeeMng.API.Models;
using EmployeeMng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class AttendanceController(IAttendanceService attendanceService)
    : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List attendance records")]
    [EndpointDescription("Filters attendance by date, employee, department, and status with pagination.")]
    [ProducesResponseType<AttendanceListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttendanceListResponse>> GetAttendance(
        [FromQuery] AttendanceListQuery query,
        CancellationToken cancellationToken)
    {
        if (query.StartDate is { } startDate &&
            query.EndDate is { } endDate &&
            startDate > endDate)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid date range",
                detail: "StartDate must be on or before EndDate.");
        }

        if (query.Date is not null &&
            (query.StartDate is not null || query.EndDate is not null))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid date filters",
                detail: "Use Date or a date range, not both.");
        }

        if (query.Page > int.MaxValue / query.PageSize)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination",
                detail: "The requested page is too large.");
        }

        return Ok(await attendanceService.GetListAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get an attendance record")]
    [ProducesResponseType<AttendanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponse>> GetAttendanceById(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await attendanceService.GetByIdAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("summary")]
    [EndpointSummary("Get attendance summary")]
    [EndpointDescription("Returns attendance counts for active employees and reports unmarked attendance separately.")]
    [ProducesResponseType<AttendanceSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceSummaryResponse>> GetSummary(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        return Ok(await attendanceService.GetSummaryAsync(date, cancellationToken));
    }

    [HttpPost]
    [EndpointSummary("Create an attendance record")]
    [ProducesResponseType<AttendanceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceResponse>> CreateAttendance(
        CreateAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await attendanceService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return ToProblem(result.ErrorCode!.Value, result.ErrorMessage!);
        }

        var response = result.Value!;
        return CreatedAtAction(
            nameof(GetAttendanceById),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update an attendance record")]
    [ProducesResponseType<AttendanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceResponse>> UpdateAttendance(
        int id,
        UpdateAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await attendanceService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : ToProblem(result.ErrorCode!.Value, result.ErrorMessage!);
    }

    [HttpPost("{id:int}/check-out")]
    [EndpointSummary("Check out an attendance record")]
    [EndpointDescription("Records the current server UTC time for an open record on its attendance date.")]
    [ProducesResponseType<AttendanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceResponse>> CheckOut(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await attendanceService.CheckOutAsync(id, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : ToProblem(result.ErrorCode!.Value, result.ErrorMessage!);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Delete an attendance record")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttendance(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await attendanceService.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return ToProblem(result.ErrorCode!.Value, result.ErrorMessage!);
        }

        return NoContent();
    }

    private ObjectResult ToProblem(AttendanceErrorCode errorCode, string detail)
    {
        var (statusCode, title) = errorCode switch
        {
            AttendanceErrorCode.Validation =>
                (StatusCodes.Status400BadRequest, "Invalid attendance request"),
            AttendanceErrorCode.NotFound or AttendanceErrorCode.EmployeeNotFound =>
                (StatusCodes.Status404NotFound, "Resource not found"),
            AttendanceErrorCode.Conflict =>
                (StatusCodes.Status409Conflict, "Attendance conflict"),
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
        };

        return Problem(statusCode: statusCode, title: title, detail: detail);
    }
}
