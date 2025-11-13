using MediatR;

namespace LMS.Application.Features.Finance.Commands.CreateMonthlyInvoice;

public class CreateMonthlyInvoiceCommand : IRequest<int>
{
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public DateTime DueDate { get; set; }
    public string? Description { get; set; }
    public int CreatedById { get; set; } // Finance or Admin user
}

