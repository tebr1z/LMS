using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Finance.Commands.ProcessWebhookPayment;

public class ProcessWebhookPaymentCommandHandler : IRequestHandler<ProcessWebhookPaymentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ProcessWebhookPaymentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ProcessWebhookPaymentCommand request, CancellationToken cancellationToken)
    {
        // Check if payment already exists with this transaction ID
        var existingPayment = await _unitOfWork.Payments.GetPaymentByProviderTransactionIdAsync(request.ProviderTransactionId);
        
        if (existingPayment != null)
        {
            // Payment already processed, update if needed
            if (existingPayment.Status != PaymentStatus.Completed)
            {
                existingPayment.Status = PaymentStatus.Completed;
                existingPayment.PaidAt = request.PaidAt ?? DateTime.UtcNow;
                existingPayment.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Payments.UpdateAsync(existingPayment);

                // Update invoice if linked
                if (existingPayment.InvoiceId.HasValue)
                {
                    var invoice = await _unitOfWork.Invoices.GetByIdAsync(existingPayment.InvoiceId.Value);
                    if (invoice != null && !invoice.PaidStatus)
                    {
                        invoice.PaidStatus = true;
                        invoice.PaidAt = request.PaidAt ?? DateTime.UtcNow;
                        invoice.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.Invoices.UpdateAsync(invoice);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // If payment doesn't exist, we need to find it by reference or create it
        // For now, we'll search by reference if provided
        Domain.Entities.Payment? payment = null;

        if (!string.IsNullOrEmpty(request.Reference))
        {
            var allPayments = await _unitOfWork.Payments.ListAsync();
            payment = allPayments.FirstOrDefault(p => 
                p.Reference == request.Reference && 
                p.Status == PaymentStatus.Pending);
        }

        if (payment == null)
        {
            // Could not find matching payment - might need to create invoice first
            // For now, log and return false or create new payment
            // In production, you'd want to handle this based on your payment flow
            throw new InvalidOperationException($"Could not find matching payment for transaction ID {request.ProviderTransactionId}. Payment reference may be missing or payment already processed.");
        }

        // Update payment with provider transaction ID and mark as completed
        payment.ProviderTransactionId = request.ProviderTransactionId;
        payment.Status = PaymentStatus.Completed;
        payment.PaidAt = request.PaidAt ?? DateTime.UtcNow;
        payment.Reference = request.Reference ?? payment.Reference;
        payment.UpdatedAt = DateTime.UtcNow;

        // Update invoice if linked
        if (payment.InvoiceId.HasValue)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(payment.InvoiceId.Value);
            if (invoice != null && !invoice.PaidStatus)
            {
                invoice.PaidStatus = true;
                invoice.PaidAt = request.PaidAt ?? DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Invoices.UpdateAsync(invoice);
            }
        }

        await _unitOfWork.Payments.UpdateAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

