using EmployeeMng.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>()
            .ToTable("Employee");
         modelBuilder.Entity<User>()
        .ToTable("User");
    }
}