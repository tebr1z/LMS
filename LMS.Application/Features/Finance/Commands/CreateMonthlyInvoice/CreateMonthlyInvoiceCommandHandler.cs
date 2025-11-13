using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Finance.Commands.CreateMonthlyInvoice;

public class CreateMonthlyInvoiceCommandHandler : IRequestHandler<CreateMonthlyInvoiceCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public CreateMonthlyInvoiceCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(CreateMonthlyInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Verify student exists
        var student = await _userRepository.GetUserByIdAsync(request.StudentId);
        if (student == null || student.Role != UserRole.Student)
        {
            throw new InvalidOperationException($"Student with ID {request.StudentId} not found.");
        }

        // Verify user is Finance or Admin
        var user = await _userRepository.GetUserByIdAsync(request.CreatedById);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        if (user.Role != UserRole.Finance && user.Role != UserRole.MasterAdmin && user.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Finance or Admin can create invoices.");
        }

        // Generate unique invoice number
        var invoiceNumber = GenerateInvoiceNumber();

        // Create invoice
        var invoice = new Invoice
        {
            StudentId = request.StudentId,
            Amount = request.Amount,
            Currency = request.Currency,
            DueDate = request.DueDate,
            PaidStatus = false,
            InvoiceNumber = invoiceNumber,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync(); // Save to get Invoice.Id

        // Create corresponding payment record
        var payment = new Payment
        {
            StudentId = request.StudentId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = PaymentMethod.Monthly,
            Status = PaymentStatus.Pending,
            DueDate = request.DueDate,
            InvoiceId = invoice.Id,
            Reference = invoiceNumber,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Payments.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        return invoice.Id;
    }

    private string GenerateInvoiceNumber()
    {
        // Generate invoice number: INV-YYYYMMDD-HHMMSS-RANDOM
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var random = new Random().Next(1000, 9999);
        return $"INV-{timestamp}-{random}";
    }
}

