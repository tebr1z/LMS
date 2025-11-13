using MediatR;

namespace LMS.Application.Features.Finance.Commands.MarkOverduePayments;

public class MarkOverduePaymentsCommand : IRequest<int> // Returns count of payments marked as Late
{
}

