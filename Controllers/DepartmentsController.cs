using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DepartmentSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentSummaryResponse>>> GetDepartments(
        CancellationToken cancellationToken)
    {
        var departmentTotals = await _context.Employees
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

        var departments = departmentTotals
            .Select(department => new DepartmentSummaryResponse(
                department.Name,
                department.EmployeeCount,
                department.ActiveEmployeeCount))
            .ToList();

        return Ok(departments);
    }
}