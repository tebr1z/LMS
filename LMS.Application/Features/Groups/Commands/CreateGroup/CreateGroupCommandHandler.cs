using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Groups.Commands.CreateGroup;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public CreateGroupCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        // Authorization check: Only MasterAdmin or Admin can create groups
        var createdByUser = await _userRepository.GetUserByIdAsync(request.CreatedById);
        if (createdByUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        if (createdByUser.Role != UserRole.MasterAdmin && createdByUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only MasterAdmin or Admin can create groups.");
        }

        var group = new Group
        {
            Name = request.Title, // Entity uses Name, command uses Title
            Description = request.Description,
            CreatedById = request.CreatedById,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Groups.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return group.Id;
    }
}

