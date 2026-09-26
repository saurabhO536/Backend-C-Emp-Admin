using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
public async Task<IActionResult> GetEmployees()
{
    var employees = await _context.Employees
        .OrderBy(e => e.Id)
        .ToListAsync();

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

