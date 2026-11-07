using System.ComponentModel.DataAnnotations;

namespace EmployeeMng.API.Models;

/// <summary>Fields used to create or update a manual invoice record.</summary>
public sealed record BillingInvoiceRequest
{
    [Required, MaxLength(100)]
    public required string InvoiceNumber { get; init; }

    [Range(0, 999999999)]
    public required decimal Amount { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "USD";

    public required DateOnly IssuedOn { get; init; }

    public DateOnly? DueOn { get; init; }

    public DateOnly? PaidOn { get; init; }

    [Required]
    [EnumDataType(typeof(InvoiceStatus))]
    public required InvoiceStatus? Status { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }
}

/// <summary>Invoice record returned by the API.</summary>
public sealed record BillingInvoiceResponse(
    int Id,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    DateOnly IssuedOn,
    DateOnly? DueOn,
    DateOnly? PaidOn,
    InvoiceStatus Status,
    string? Description);