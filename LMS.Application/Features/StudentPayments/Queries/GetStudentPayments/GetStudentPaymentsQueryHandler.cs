using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.StudentPayments.Queries.GetStudentPayments;

public class GetStudentPaymentsQueryHandler : IRequestHandler<GetStudentPaymentsQuery, StudentPaymentsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetStudentPaymentsQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<StudentPaymentsDto> Handle(GetStudentPaymentsQuery request, CancellationToken cancellationToken)
    {
        // Verify student exists
        var student = await _userRepository.GetUserByIdAsync(request.StudentId);
        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {request.StudentId} not found.");
        }

        // Get invoices
        var invoices = await _unitOfWork.Invoices.GetInvoicesByStudentIdAsync(request.StudentId);
        var invoiceDtos = invoices.Select(i => new InvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            Amount = i.Amount,
            Currency = i.Currency,
            DueDate = i.DueDate,
            PaidStatus = i.PaidStatus,
            PaidAt = i.PaidAt,
            Description = i.Description,
            CreatedAt = i.CreatedAt
        }).ToList();

        // Get payments
        var payments = await _unitOfWork.Payments.GetPaymentsByStudentIdAsync(request.StudentId);
        var paymentDtos = new List<PaymentSummaryDto>();

        foreach (var payment in payments)
        {
            var invoice = payment.InvoiceId.HasValue
                ? await _unitOfWork.Invoices.GetByIdAsync(payment.InvoiceId.Value)
                : null;

            paymentDtos.Add(new PaymentSummaryDto
            {
                Id = payment.Id,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                PaymentMethodName = payment.PaymentMethod.ToString(),
                Status = payment.Status,
                StatusName = payment.Status.ToString(),
                DueDate = payment.DueDate,
                PaidAt = payment.PaidAt,
                Reference = payment.Reference,
                InvoiceNumber = invoice?.InvoiceNumber,
                CreatedAt = payment.CreatedAt
            });
        }

        // Calculate summary
        var summary = new PaymentSummary
        {
            TotalAmount = payments.Sum(p => p.Amount),
            PaidAmount = payments
                .Where(p => p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Manual)
                .Sum(p => p.Amount),
            PendingAmount = payments
                .Where(p => p.Status == PaymentStatus.Pending)
                .Sum(p => p.Amount),
            OverdueAmount = payments
                .Where(p => p.Status == PaymentStatus.Late)
                .Sum(p => p.Amount),
            TotalPayments = payments.Count,
            PaidPayments = payments.Count(p => p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Manual),
            PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending),
            OverduePayments = payments.Count(p => p.Status == PaymentStatus.Late)
        };

        var studentName = await _userRepository.GetUserFullNameAsync(request.StudentId) ?? "Unknown";

        return new StudentPaymentsDto
        {
            StudentId = request.StudentId,
            StudentName = studentName,
            Invoices = invoiceDtos,
            Payments = paymentDtos,
            Summary = summary
        };
    }
}

