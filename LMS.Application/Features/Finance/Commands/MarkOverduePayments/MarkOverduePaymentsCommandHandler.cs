using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Finance.Commands.MarkOverduePayments;

public class MarkOverduePaymentsCommandHandler : IRequestHandler<MarkOverduePaymentsCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkOverduePaymentsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(MarkOverduePaymentsCommand request, CancellationToken cancellationToken)
    {
        // Get all overdue payments
        var overduePayments = await _unitOfWork.Payments.GetOverduePaymentsAsync();

        var count = 0;
        foreach (var payment in overduePayments)
        {
            // Only mark as Late if not already completed, manual, or deferred
            if (payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Late;
                payment.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Payments.UpdateAsync(payment);
                count++;
            }
        }

        // Also mark overdue invoices
        var overdueInvoices = await _unitOfWork.Invoices.GetOverdueInvoicesAsync();
        foreach (var invoice in overdueInvoices)
        {
            // Update invoice updated timestamp (invoices don't have status, only PaidStatus)
            invoice.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Invoices.UpdateAsync(invoice);
        }

        await _unitOfWork.SaveChangesAsync();

        return count;
    }
}

