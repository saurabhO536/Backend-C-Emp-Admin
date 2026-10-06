using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public EmployeesController(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
public async Task<IActionResult> GetEmployees()
{
    const string cacheKey = "employees";

    if (_cache.TryGetValue(cacheKey, out List<Employee>? employees))
    {
        return Ok(employees);
    }

    employees = await _context.Employees
        .AsNoTracking()
        .ToListAsync();

    var cacheOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };

    _cache.Set(cacheKey, employees, cacheOptions);

    return Ok(employees);
}

    [HttpGet("{id}")]
public async Task<IActionResult> GetEmployee(int id)
{
    var employee = await _context.Employees.FindAsync(id);

    if (employee == null)
    {
        return NotFound();
    }

    return Ok(employee);
}
  [HttpPost]
public async Task<IActionResult> CreateEmployee(Employee employee)
{
    _context.Employees.Add(employee);

    await _context.SaveChangesAsync();
    _cache.Remove("employees");

    return CreatedAtAction(
        nameof(GetEmployee),
        new { id = employee.Id },
        employee);
}

[HttpPut("{id}")]
public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
{
    var existingEmployee = await _context.Employees.FindAsync(id);

    if (existingEmployee == null)
    {
        return NotFound();
    }

    existingEmployee.FirstName = employee.FirstName;
    existingEmployee.LastName = employee.LastName;
    existingEmployee.Email = employee.Email;
    existingEmployee.Department = employee.Department;
    existingEmployee.Salary = employee.Salary;

    await _context.SaveChangesAsync();

    return Ok(existingEmployee);
}
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteEmployee(int id)
{
    var employee = await _context.Employees.FindAsync(id);

    if (employee == null)
    {
        return NotFound();
    }

    _context.Employees.Remove(employee);

    await _context.SaveChangesAsync();

    return NoContent();
}

}

