using EmployeeMng.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>()
            .ToTable("Employee");
         modelBuilder.Entity<User>()
             .ToTable("User");

         modelBuilder.Entity<Attendance>(entity =>
         {
             entity.ToTable("Attendance");
             entity.Property(attendance => attendance.AttendanceDate)
                 .HasColumnType("date");
             entity.Property(attendance => attendance.Status)
                 .HasConversion<string>()
                 .HasMaxLength(16);
             entity.Property(attendance => attendance.Notes)
                 .HasColumnType("text");
             entity.Property(attendance => attendance.CheckOutTime)
                 .IsConcurrencyToken();
             entity.HasIndex(attendance => new
                 {
                     attendance.EmployeeId,
                     attendance.AttendanceDate
                 })
                 .IsUnique();
             entity.HasOne(attendance => attendance.Employee)
                 .WithMany()
                 .HasForeignKey(attendance => attendance.EmployeeId)
                 .OnDelete(DeleteBehavior.Restrict);
         });
    }
}