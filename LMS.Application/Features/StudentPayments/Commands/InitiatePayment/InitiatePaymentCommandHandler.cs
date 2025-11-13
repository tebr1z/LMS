using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace LMS.Application.Features.StudentPayments.Commands.InitiatePayment;

public class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, PaymentSessionDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public InitiatePaymentCommandHandler(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<PaymentSessionDto> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        // Get payment
        var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId);
        if (payment == null)
        {
            throw new InvalidOperationException($"Payment with ID {request.PaymentId} not found.");
        }

        // Verify payment belongs to student
        if (payment.StudentId != request.StudentId)
        {
            throw new UnauthorizedAccessException("Payment does not belong to this student.");
        }

        // Check if payment is already completed
        if (payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Manual)
        {
            throw new InvalidOperationException("Payment has already been completed.");
        }

        // Check if payment is deferred
        if (payment.Status == PaymentStatus.Deferred)
        {
            throw new InvalidOperationException("Payment has been deferred. Please contact finance office.");
        }

        // Generate payment session ID
        var paymentSessionId = Guid.NewGuid().ToString();

        // In production, integrate with actual payment provider (OneBank, etc.)
        // For now, we'll simulate the integration
        var providerApiUrl = _configuration["PaymentProvider:ApiUrl"] ?? "https://api.onebank.az/v1/payments";
        var merchantId = _configuration["PaymentProvider:MerchantId"] ?? "YOUR_MERCHANT_ID";
        
        // Create payment session with provider (simulated)
        // In production, you would make an HTTP call to the provider's API
        var providerSessionId = $"PROV_{paymentSessionId}";
        
        // Generate redirect URL (in production, this comes from the provider)
        var redirectUrl = $"{providerApiUrl}/session/{providerSessionId}";

        // Update payment with session reference (optional - you might store this separately)
        payment.Reference = paymentSessionId;
        payment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Payments.UpdateAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        // Session expires in 30 minutes
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        return new PaymentSessionDto
        {
            PaymentSessionId = paymentSessionId,
            RedirectUrl = redirectUrl,
            ProviderSessionId = providerSessionId,
            ExpiresAt = expiresAt
        };
    }
}

