using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[Authorize]
[ApiController]
[Route("api/billing/invoices")]
public sealed class BillingController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BillingInvoiceResponse>>> GetInvoices(
        [FromQuery] InvoiceStatus? status,
        CancellationToken cancellationToken)
    {
        var query = context.BillingInvoices.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(invoice => invoice.Status == status);
        }

        var invoices = await query
            .OrderByDescending(invoice => invoice.IssuedOn)
            .ToListAsync(cancellationToken);
        return Ok(invoices.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BillingInvoiceResponse>> GetInvoice(
        int id,
        CancellationToken cancellationToken)
    {
        var invoice = await context.BillingInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return invoice is null ? NotFound() : Ok(ToResponse(invoice));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BillingInvoiceResponse>> CreateInvoice(
        BillingInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoiceNumber = request.InvoiceNumber.Trim();
        if (await context.BillingInvoices.AnyAsync(
                invoice => invoice.InvoiceNumber == invoiceNumber,
                cancellationToken))
        {
            return InvoiceConflict();
        }

        if (!ValidInvoiceDates(request.IssuedOn, request.DueOn, request.PaidOn))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid invoice dates",
                detail: "DueOn and PaidOn cannot be earlier than IssuedOn.");
        }

        var invoice = new BillingInvoice
        {
            InvoiceNumber = invoiceNumber,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            IssuedOn = request.IssuedOn,
            DueOn = request.DueOn,
            PaidOn = request.PaidOn,
            Status = request.Status!.Value,
            Description = request.Description?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.BillingInvoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, ToResponse(invoice));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BillingInvoiceResponse>> UpdateInvoice(
        int id,
        BillingInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await context.BillingInvoices
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var invoiceNumber = request.InvoiceNumber.Trim();
        if (await context.BillingInvoices.AnyAsync(
                item => item.Id != id && item.InvoiceNumber == invoiceNumber,
                cancellationToken))
        {
            return InvoiceConflict();
        }

        if (!ValidInvoiceDates(request.IssuedOn, request.DueOn, request.PaidOn))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid invoice dates",
                detail: "DueOn and PaidOn cannot be earlier than IssuedOn.");
        }

        invoice.InvoiceNumber = invoiceNumber;
        invoice.Amount = request.Amount;
        invoice.Currency = request.Currency.ToUpperInvariant();
        invoice.IssuedOn = request.IssuedOn;
        invoice.DueOn = request.DueOn;
        invoice.PaidOn = request.PaidOn;
        invoice.Status = request.Status!.Value;
        invoice.Description = request.Description?.Trim();
        await context.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(invoice));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteInvoice(int id, CancellationToken cancellationToken)
    {
        var invoice = await context.BillingInvoices
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        context.BillingInvoices.Remove(invoice);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ObjectResult InvoiceConflict() => Conflict(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Invoice number already exists"
    });

    private static bool ValidInvoiceDates(DateOnly issuedOn, DateOnly? dueOn, DateOnly? paidOn) =>
        (dueOn is null || dueOn >= issuedOn) &&
        (paidOn is null || paidOn >= issuedOn);

    private static BillingInvoiceResponse ToResponse(BillingInvoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.Amount,
        invoice.Currency,
        invoice.IssuedOn,
        invoice.DueOn,
        invoice.PaidOn,
        invoice.Status,
        invoice.Description);
}