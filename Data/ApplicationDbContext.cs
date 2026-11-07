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
    public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
    public DbSet<OrganizationSubscription> OrganizationSubscriptions { get; set; } = null!;
    public DbSet<BillingInvoice> BillingInvoices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>()
            .ToTable("Employee");
         modelBuilder.Entity<User>()
             .ToTable("User");
         modelBuilder.Entity<User>()
             .HasIndex(user => user.EmployeeId)
             .IsUnique();
         modelBuilder.Entity<User>()
             .HasOne<Employee>()
             .WithOne()
             .HasForeignKey<User>(user => user.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);

         modelBuilder.Entity<LeaveRequest>(entity =>
         {
             entity.ToTable("LeaveRequest");
             entity.Property(request => request.Status)
                 .HasConversion<string>()
                 .HasMaxLength(16);
             entity.Property(request => request.LeaveType)
                 .HasMaxLength(100);
             entity.Property(request => request.Reason)
                 .HasMaxLength(2000);
             entity.Property(request => request.ReviewNotes)
                 .HasMaxLength(2000);
             entity.HasOne<Employee>()
                 .WithMany()
                 .HasForeignKey(request => request.EmployeeId)
                 .OnDelete(DeleteBehavior.Restrict);
             entity.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(request => request.ReviewedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);
             entity.HasIndex(request => new
             {
                 request.EmployeeId,
                 request.StartDate,
                 request.EndDate
             });
         });

         modelBuilder.Entity<SubscriptionPlan>(entity =>
         {
             entity.ToTable("SubscriptionPlan");
             entity.Property(plan => plan.MonthlyPrice).HasPrecision(12, 2);
             entity.Property(plan => plan.Currency).HasMaxLength(3);
             entity.Property(plan => plan.Name).HasMaxLength(120);
             entity.HasIndex(plan => plan.Name).IsUnique();
         });

         modelBuilder.Entity<OrganizationSubscription>(entity =>
         {
             entity.ToTable("OrganizationSubscription");
             entity.Property(subscription => subscription.Status)
                 .HasConversion<string>()
                 .HasMaxLength(16);
             entity.HasOne<SubscriptionPlan>()
                 .WithMany()
                 .HasForeignKey(subscription => subscription.PlanId)
                 .OnDelete(DeleteBehavior.Restrict);
         });

         modelBuilder.Entity<BillingInvoice>(entity =>
         {
             entity.ToTable("BillingInvoice");
             entity.Property(invoice => invoice.InvoiceNumber).HasMaxLength(100);
             entity.Property(invoice => invoice.Amount).HasPrecision(12, 2);
             entity.Property(invoice => invoice.Currency).HasMaxLength(3);
             entity.Property(invoice => invoice.Status)
                 .HasConversion<string>()
                 .HasMaxLength(16);
             entity.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
         });

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