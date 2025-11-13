using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.Notifications.MarkAsReadAsync(request.NotificationId, request.UserId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

