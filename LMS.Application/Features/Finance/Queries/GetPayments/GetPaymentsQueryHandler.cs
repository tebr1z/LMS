using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Finance.Queries.GetPayments;

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, IEnumerable<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetPaymentsQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        List<Domain.Entities.Payment> payments;

        if (request.StudentId.HasValue && request.Status.HasValue)
        {
            var studentPayments = await _unitOfWork.Payments.GetPaymentsByStudentIdAsync(request.StudentId.Value);
            payments = studentPayments.Where(p => p.Status == request.Status.Value).ToList();
        }
        else if (request.StudentId.HasValue)
        {
            payments = await _unitOfWork.Payments.GetPaymentsByStudentIdAsync(request.StudentId.Value);
        }
        else if (request.Status.HasValue)
        {
            payments = await _unitOfWork.Payments.GetPaymentsByStatusAsync(request.Status.Value);
        }
        else
        {
            payments = await _unitOfWork.Payments.ListAsync();
        }

        // Filter by PaymentMethod if specified
        if (request.PaymentMethod.HasValue)
        {
            payments = payments.Where(p => p.PaymentMethod == request.PaymentMethod.Value).ToList();
        }

        var paymentDtos = new List<PaymentDto>();

        foreach (var payment in payments.OrderByDescending(p => p.CreatedAt))
        {
            var invoice = payment.InvoiceId.HasValue 
                ? await _unitOfWork.Invoices.GetByIdAsync(payment.InvoiceId.Value)
                : null;

            paymentDtos.Add(new PaymentDto
            {
                Id = payment.Id,
                StudentId = payment.StudentId,
                StudentName = await _userRepository.GetUserFullNameAsync(payment.StudentId) ?? "Unknown",
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                PaymentMethodName = payment.PaymentMethod.ToString(),
                Status = payment.Status,
                StatusName = payment.Status.ToString(),
                PaidAt = payment.PaidAt,
                DueDate = payment.DueDate,
                Reference = payment.Reference,
                ProviderTransactionId = payment.ProviderTransactionId,
                Notes = payment.Notes,
                InvoiceId = payment.InvoiceId,
                InvoiceNumber = invoice?.InvoiceNumber,
                CreatedAt = payment.CreatedAt
            });
        }

        return paymentDtos;
    }
}

