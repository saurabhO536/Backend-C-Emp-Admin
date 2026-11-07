namespace EmployeeMng.API.Models;

public enum InvoiceStatus
{
    Draft,
    Open,
    Paid,
    Overdue,
    Void
}

public class BillingInvoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public DateOnly IssuedOn { get; set; }

    public DateOnly? DueOn { get; set; }

    public DateOnly? PaidOn { get; set; }

    public InvoiceStatus Status { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}