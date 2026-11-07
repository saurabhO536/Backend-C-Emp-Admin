using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/subscriptions")]
public sealed class SubscriptionsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationSubscriptionResponse>>> GetSubscriptions(
        CancellationToken cancellationToken)
    {
        var subscriptions = await context.OrganizationSubscriptions
            .AsNoTracking()
            .Join(context.SubscriptionPlans.AsNoTracking(),
                subscription => subscription.PlanId,
                plan => plan.Id,
                (subscription, plan) => new OrganizationSubscriptionResponse(
                    subscription.Id,
                    subscription.PlanId,
                    plan.Name,
                    plan.MonthlyPrice,
                    plan.Currency,
                    subscription.Status,
                    subscription.StartsOn,
                    subscription.EndsOn,
                    subscription.NextRenewalDate))
            .OrderByDescending(subscription => subscription.StartsOn)
            .ToListAsync(cancellationToken);

        return Ok(subscriptions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrganizationSubscriptionResponse>> GetSubscription(
        int id,
        CancellationToken cancellationToken)
    {
        var subscription = await context.OrganizationSubscriptions
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Join(context.SubscriptionPlans.AsNoTracking(),
                item => item.PlanId,
                plan => plan.Id,
                (item, plan) => new OrganizationSubscriptionResponse(
                    item.Id,
                    item.PlanId,
                    plan.Name,
                    plan.MonthlyPrice,
                    plan.Currency,
                    item.Status,
                    item.StartsOn,
                    item.EndsOn,
                    item.NextRenewalDate))
            .FirstOrDefaultAsync(cancellationToken);
        return subscription is null ? NotFound() : Ok(subscription);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrganizationSubscriptionResponse>> CreateSubscription(
        OrganizationSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EndsOn is not null && request.EndsOn < request.StartsOn)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid subscription dates",
                detail: "EndsOn must be on or after StartsOn.");
        }

        var plan = await context.SubscriptionPlans
            .FirstOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive, cancellationToken);
        if (plan is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Active subscription plan not found"
            });
        }

        var hasLiveSubscription = await context.OrganizationSubscriptions.AnyAsync(
            subscription => subscription.Status != SubscriptionStatus.Canceled,
            cancellationToken);
        if (hasLiveSubscription && request.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing or SubscriptionStatus.PastDue)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "A live subscription already exists"
            });
        }

        var subscription = new OrganizationSubscription
        {
            PlanId = plan.Id,
            Status = request.Status!.Value,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            NextRenewalDate = request.NextRenewalDate,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.OrganizationSubscriptions.Add(subscription);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, ToResponse(subscription, plan));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrganizationSubscriptionResponse>> UpdateSubscription(
        int id,
        OrganizationSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EndsOn is not null && request.EndsOn < request.StartsOn)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid subscription dates",
                detail: "EndsOn must be on or after StartsOn.");
        }

        var subscription = await context.OrganizationSubscriptions
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (subscription is null)
        {
            return NotFound();
        }

        var plan = await context.SubscriptionPlans
            .FirstOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive, cancellationToken);
        if (plan is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Active subscription plan not found"
            });
        }

        var createsSecondLiveSubscription = request.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing or SubscriptionStatus.PastDue;
        if (createsSecondLiveSubscription && await context.OrganizationSubscriptions.AnyAsync(
                item => item.Id != id && item.Status != SubscriptionStatus.Canceled,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "A live subscription already exists"
            });
        }

        subscription.PlanId = plan.Id;
        subscription.Status = request.Status!.Value;
        subscription.StartsOn = request.StartsOn;
        subscription.EndsOn = request.EndsOn;
        subscription.NextRenewalDate = request.NextRenewalDate;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(subscription, plan));
    }

    private static OrganizationSubscriptionResponse ToResponse(
        OrganizationSubscription subscription,
        SubscriptionPlan plan) => new(
        subscription.Id,
        plan.Id,
        plan.Name,
        plan.MonthlyPrice,
        plan.Currency,
        subscription.Status,
        subscription.StartsOn,
        subscription.EndsOn,
        subscription.NextRenewalDate);
}