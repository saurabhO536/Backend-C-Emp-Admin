using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/users")]
public sealed class UsersController(ApplicationDbContext context) : ControllerBase
{
    [HttpPut("{userId:int}/employee")]
    public async Task<IActionResult> LinkEmployee(
        int userId,
        LinkEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var employeeExists = await context.Employees
            .AnyAsync(employee => employee.Id == request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee not found"
            });
        }

        var alreadyLinked = await context.Users.AnyAsync(
            item => item.Id != userId && item.EmployeeId == request.EmployeeId,
            cancellationToken);
        if (alreadyLinked)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Employee already linked",
                Detail = "An employee record can be linked to only one user account."
            });
        }

        user.EmployeeId = request.EmployeeId;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:int}/employee")]
    public async Task<IActionResult> UnlinkEmployee(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.EmployeeId = null;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}