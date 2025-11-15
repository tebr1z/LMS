using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Finance.Commands.AcceptManualPayment;

public class AcceptManualPaymentCommandHandler : IRequestHandler<AcceptManualPaymentCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AcceptManualPaymentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(AcceptManualPaymentCommand request, CancellationToken cancellationToken)
    {
        // Get payment
        var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId);
        if (payment == null)
        {
            throw new InvalidOperationException($"Payment with ID {request.PaymentId} not found.");
        }

        // Verify user is Finance or Admin
        var user = await _userRepository.GetUserByIdAsync(request.AcceptedById);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        if (user.Role != UserRole.Finance && user.Role != UserRole.MasterAdmin && user.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Finance or Admin can accept manual payments.");
        }

        // Mark payment as Manual and Completed
        payment.Status = PaymentStatus.Manual;
        payment.PaidAt = DateTime.UtcNow;
        payment.Reference = request.Reference ?? payment.Reference;
        payment.Notes = request.Notes ?? payment.Notes;
        payment.UpdatedAt = DateTime.UtcNow;

        // Update invoice if linked
        if (payment.InvoiceId.HasValue)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(payment.InvoiceId.Value);
            if (invoice != null)
            {
                invoice.PaidStatus = true;
                invoice.PaidAt = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Invoices.UpdateAsync(invoice);
            }
        }

        await _unitOfWork.Payments.UpdateAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        // Award reward points for on-time payment
        await AwardRewardPointsForOnTimePaymentAsync(payment, cancellationToken);

        return payment.Id;
    }

    private async Task AwardRewardPointsForOnTimePaymentAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            // Check if payment was made on time (PaidAt <= DueDate)
            if (payment.PaidAt.HasValue && payment.DueDate.HasValue)
            {
                if (payment.PaidAt.Value <= payment.DueDate.Value)
                {
                    // Payment was on time - award 50 reward points
                    var rewardPoint = new Domain.Entities.RewardPoint
                    {
                        UserId = payment.StudentId,
                        Points = 50,
                        Reason = $"On-time payment reward: Payment #{payment.Id} completed on or before due date",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.RewardPoints.AddAsync(rewardPoint);
                    await _unitOfWork.SaveChangesAsync();
                }
                // If PaidAt > DueDate, no points are awarded
            }
        }
        catch (Exception)
        {
            // Don't fail payment processing if reward points fail
            // Log error but continue
        }
    }
}

