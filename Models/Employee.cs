using EmployeeMng.API.Data;
using Microsoft.EntityFrameworkCore;
namespace EmployeeMng.API.Models;

public class Employee
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public decimal Salary { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsActive { get; set; }
}