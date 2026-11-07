using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/subscription-plans")]
public sealed class SubscriptionPlansController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>> GetPlans(
        CancellationToken cancellationToken)
    {
        var query = context.SubscriptionPlans.AsNoTracking();
        if (!User.IsInRole("Admin"))
        {
            query = query.Where(plan => plan.IsActive);
        }

        var plans = await query
            .OrderBy(plan => plan.MonthlyPrice)
            .Select(plan => new SubscriptionPlanResponse(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.MonthlyPrice,
                plan.Currency,
                plan.SeatLimit,
                plan.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubscriptionPlanResponse>> GetPlan(
        int id,
        CancellationToken cancellationToken)
    {
        var plan = await context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null || (!User.IsInRole("Admin") && !plan.IsActive))
        {
            return NotFound();
        }

        return Ok(ToResponse(plan));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubscriptionPlanResponse>> CreatePlan(
        SubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await context.SubscriptionPlans.AnyAsync(plan => plan.Name == name, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Plan name already exists"
            });
        }

        var plan = new SubscriptionPlan
        {
            Name = name,
            Description = request.Description?.Trim(),
            MonthlyPrice = request.MonthlyPrice,
            Currency = request.Currency.ToUpperInvariant(),
            SeatLimit = request.SeatLimit,
            IsActive = request.IsActive
        };

        context.SubscriptionPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPlan), new { id = plan.Id }, ToResponse(plan));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubscriptionPlanResponse>> UpdatePlan(
        int id,
        SubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await context.SubscriptionPlans
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (await context.SubscriptionPlans.AnyAsync(
                item => item.Id != id && item.Name == name,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Plan name already exists"
            });
        }

        plan.Name = name;
        plan.Description = request.Description?.Trim();
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.Currency = request.Currency.ToUpperInvariant();
        plan.SeatLimit = request.SeatLimit;
        plan.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(plan));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePlan(int id, CancellationToken cancellationToken)
    {
        var plan = await context.SubscriptionPlans
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (await context.OrganizationSubscriptions.AnyAsync(
                subscription => subscription.PlanId == id,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Plan is in use",
                Detail = "Deactivate this plan instead of deleting a plan used by a subscription."
            });
        }

        context.SubscriptionPlans.Remove(plan);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static SubscriptionPlanResponse ToResponse(SubscriptionPlan plan) => new(
        plan.Id,
        plan.Name,
        plan.Description,
        plan.MonthlyPrice,
        plan.Currency,
        plan.SeatLimit,
        plan.IsActive);
}