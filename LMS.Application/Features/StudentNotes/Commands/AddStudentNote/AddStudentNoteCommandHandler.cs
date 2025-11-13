using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.StudentNotes.Commands.AddStudentNote;

public class AddStudentNoteCommandHandler : IRequestHandler<AddStudentNoteCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AddStudentNoteCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(AddStudentNoteCommand request, CancellationToken cancellationToken)
    {
        // Verify group exists
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found.");
        }

        // Verify student exists and belongs to group
        var isStudentInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(request.GroupId, request.StudentId);
        if (!isStudentInGroup)
        {
            throw new InvalidOperationException($"Student with ID {request.StudentId} is not a member of group {request.GroupId}.");
        }

        // Verify user creating note
        var creator = await _userRepository.GetUserByIdAsync(request.CreatedById);
        if (creator == null)
        {
            throw new UnauthorizedAccessException("Invalid user creating note.");
        }

        // Verify creator belongs to group and has appropriate role (Mentor, Teacher, or StudentOffice)
        var isCreatorInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(request.GroupId, request.CreatedById);
        if (!isCreatorInGroup)
        {
            throw new UnauthorizedAccessException("User creating note must belong to the group.");
        }

        // Check if user has appropriate role (Mentor, Teacher, StudentOffice, or Admin)
        var groupUser = await _unitOfWork.GroupUsers.GetByGroupAndUserAsync(request.GroupId, request.CreatedById);
        bool canAddNote = creator.Role == UserRole.MasterAdmin || 
                         creator.Role == UserRole.Admin ||
                         creator.Role == UserRole.StudentOffice ||
                         (groupUser != null && (groupUser.Role == GroupRole.Mentor || groupUser.Role == GroupRole.Teacher));

        if (!canAddNote)
        {
            throw new UnauthorizedAccessException("Only Mentor, Teacher, StudentOffice, or Admin can add student notes.");
        }

        // Create student note
        var note = new StudentNote
        {
            StudentId = request.StudentId,
            GroupId = request.GroupId,
            Title = request.Title,
            Content = request.Content,
            IsPrivate = request.IsPrivate,
            IsImportant = request.IsImportant,
            CreatedById = request.CreatedById,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StudentNotes.AddAsync(note);
        await _unitOfWork.SaveChangesAsync();

        return note.Id;
    }
}

