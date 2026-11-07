using System.Security.Claims;
using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/leave-requests")]
public sealed class LeaveRequestsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestResponse>>> GetLeaveRequests(
        CancellationToken cancellationToken)
    {
        var query = context.LeaveRequests.AsNoTracking();
        if (!User.IsInRole("Admin"))
        {
            var user = await GetCurrentUser(cancellationToken);
            if (user?.EmployeeId is not int employeeId)
            {
                return Ok(Array.Empty<LeaveRequestResponse>());
            }

            query = query.Where(request => request.EmployeeId == employeeId);
        }

        var requests = await query
            .Join(context.Employees.AsNoTracking(),
                request => request.EmployeeId,
                employee => employee.Id,
                (request, employee) => new LeaveRequestResponse(
                    request.Id,
                    request.EmployeeId,
                    employee.FirstName + " " + employee.LastName,
                    request.StartDate,
                    request.EndDate,
                    request.LeaveType,
                    request.Reason,
                    request.Status,
                    request.CreatedAt,
                    request.ReviewedByUserId,
                    request.ReviewedAt,
                    request.ReviewNotes))
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestResponse>> GetLeaveRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var request = await context.LeaveRequests
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Join(context.Employees.AsNoTracking(),
                item => item.EmployeeId,
                employee => employee.Id,
                (item, employee) => new LeaveRequestResponse(
                    item.Id,
                    item.EmployeeId,
                    employee.FirstName + " " + employee.LastName,
                    item.StartDate,
                    item.EndDate,
                    item.LeaveType,
                    item.Reason,
                    item.Status,
                    item.CreatedAt,
                    item.ReviewedByUserId,
                    item.ReviewedAt,
                    item.ReviewNotes))
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin"))
        {
            var user = await GetCurrentUser(cancellationToken);
            if (user?.EmployeeId != request.EmployeeId)
            {
                return NotFound();
            }
        }

        return Ok(request);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestResponse>> CreateLeaveRequest(
        CreateLeaveRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid leave dates",
                detail: "StartDate must be on or before EndDate.");
        }

        var user = await GetCurrentUser(cancellationToken);
        if (user?.EmployeeId is not int employeeId)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Employee account not linked",
                detail: "Ask an administrator to link your account to an employee record.");
        }

        var hasOverlap = await context.LeaveRequests.AnyAsync(existing =>
            existing.EmployeeId == employeeId &&
            existing.Status != LeaveRequestStatus.Rejected &&
            existing.StartDate <= request.EndDate &&
            existing.EndDate >= request.StartDate,
            cancellationToken);

        if (hasOverlap)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Overlapping leave request",
                detail: "An overlapping pending or approved leave request already exists.");
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            LeaveType = request.LeaveType.Trim(),
            Reason = request.Reason.Trim(),
            Status = LeaveRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.LeaveRequests.Add(leaveRequest);
        await context.SaveChangesAsync(cancellationToken);

        var employee = await context.Employees
            .AsNoTracking()
            .FirstAsync(item => item.Id == employeeId, cancellationToken);
        var response = new LeaveRequestResponse(
            leaveRequest.Id,
            leaveRequest.EmployeeId,
            $"{employee.FirstName} {employee.LastName}",
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.LeaveType,
            leaveRequest.Reason,
            leaveRequest.Status,
            leaveRequest.CreatedAt,
            leaveRequest.ReviewedByUserId,
            leaveRequest.ReviewedAt,
            leaveRequest.ReviewNotes);

        return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, response);
    }

    [HttpPost("{id:int}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LeaveRequestResponse>> ReviewLeaveRequest(
        int id,
        ReviewLeaveRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Decision is not (LeaveRequestStatus.Approved or LeaveRequestStatus.Rejected))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid leave decision",
                detail: "Decision must be Approved or Rejected.");
        }

        var leaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (leaveRequest is null)
        {
            return NotFound();
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Leave request already reviewed",
                detail: "Only pending leave requests can be reviewed.");
        }

        leaveRequest.Status = request.Decision.Value;
        leaveRequest.ReviewedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        leaveRequest.ReviewedAt = DateTimeOffset.UtcNow;
        leaveRequest.ReviewNotes = request.Notes?.Trim();
        await context.SaveChangesAsync(cancellationToken);

        return await GetLeaveRequest(id, cancellationToken);
    }

    private Task<User?> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return context.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}