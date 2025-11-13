using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Finance.Commands.DeferPayment;

public class DeferPaymentCommandHandler : IRequestHandler<DeferPaymentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public DeferPaymentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(DeferPaymentCommand request, CancellationToken cancellationToken)
    {
        // Get payment
        var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId);
        if (payment == null)
        {
            throw new InvalidOperationException($"Payment with ID {request.PaymentId} not found.");
        }

        // Verify user is Finance or Admin
        var user = await _userRepository.GetUserByIdAsync(request.DeferredById);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        if (user.Role != UserRole.Finance && user.Role != UserRole.MasterAdmin && user.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Finance or Admin can defer payments.");
        }

        // Mark payment as Deferred (fiancé status)
        payment.Status = PaymentStatus.Deferred;
        payment.Notes = request.Notes ?? payment.Notes;
        payment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Payments.UpdateAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

