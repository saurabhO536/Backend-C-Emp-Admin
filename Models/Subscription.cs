namespace EmployeeMng.API.Models;

public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled
}

public class SubscriptionPlan
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal MonthlyPrice { get; set; }

    public string Currency { get; set; } = "USD";

    public int? SeatLimit { get; set; }

    public bool IsActive { get; set; } = true;
}

public class OrganizationSubscription
{
    public int Id { get; set; }

    public int PlanId { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateOnly StartsOn { get; set; }

    public DateOnly? EndsOn { get; set; }

    public DateOnly? NextRenewalDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}