using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.StudentOffice.Commands.FlagStudent;

public class FlagStudentCommandHandler : IRequestHandler<FlagStudentCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public FlagStudentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(FlagStudentCommand request, CancellationToken cancellationToken)
    {
        // Verify student exists
        var student = await _userRepository.GetUserByIdAsync(request.StudentId);
        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {request.StudentId} not found.");
        }

        if (student.Role != UserRole.Student)
        {
            throw new InvalidOperationException($"User with ID {request.StudentId} is not a student.");
        }

        // Verify user creating flag is StudentOffice or Admin
        var creator = await _userRepository.GetUserByIdAsync(request.CreatedById);
        if (creator == null)
        {
            throw new UnauthorizedAccessException("Invalid user creating flag.");
        }

        if (creator.Role != UserRole.StudentOffice && 
            creator.Role != UserRole.MasterAdmin && 
            creator.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only StudentOffice or Admin can flag students.");
        }

        // Create student flag
        var flag = new StudentFlag
        {
            StudentId = request.StudentId,
            CreatedById = request.CreatedById,
            Reason = request.Reason,
            RecommendedAction = request.RecommendedAction,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StudentFlags.AddAsync(flag);
        await _unitOfWork.SaveChangesAsync();

        return flag.Id;
    }
}

