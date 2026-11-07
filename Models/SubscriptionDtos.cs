using System.ComponentModel.DataAnnotations;

namespace EmployeeMng.API.Models;

/// <summary>Fields used to create or update a subscription plan.</summary>
public sealed record SubscriptionPlanRequest
{
    [Required, MaxLength(120)]
    public required string Name { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(0, 999999999)]
    public required decimal MonthlyPrice { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "USD";

    [Range(1, int.MaxValue)]
    public int? SeatLimit { get; init; }

    public bool IsActive { get; init; } = true;
}

/// <summary>Fields used to assign a plan to the organization.</summary>
public sealed record OrganizationSubscriptionRequest
{
    [Range(1, int.MaxValue)]
    public required int PlanId { get; init; }

    [Required]
    [EnumDataType(typeof(SubscriptionStatus))]
    public required SubscriptionStatus? Status { get; init; }

    public required DateOnly StartsOn { get; init; }

    public DateOnly? EndsOn { get; init; }

    public DateOnly? NextRenewalDate { get; init; }
}

/// <summary>Subscription plan returned by the API.</summary>
public sealed record SubscriptionPlanResponse(
    int Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    string Currency,
    int? SeatLimit,
    bool IsActive);

/// <summary>Organization subscription returned by the API.</summary>
public sealed record OrganizationSubscriptionResponse(
    int Id,
    int PlanId,
    string PlanName,
    decimal MonthlyPrice,
    string Currency,
    SubscriptionStatus Status,
    DateOnly StartsOn,
    DateOnly? EndsOn,
    DateOnly? NextRenewalDate);