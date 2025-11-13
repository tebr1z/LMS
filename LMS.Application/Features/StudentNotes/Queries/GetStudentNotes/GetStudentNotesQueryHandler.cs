using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.StudentNotes.Queries.GetStudentNotes;

public class GetStudentNotesQueryHandler : IRequestHandler<GetStudentNotesQuery, IEnumerable<StudentNoteDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetStudentNotesQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<StudentNoteDto>> Handle(GetStudentNotesQuery request, CancellationToken cancellationToken)
    {
        // Verify user requesting notes
        var user = await _userRepository.GetUserByIdAsync(request.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        List<Domain.Entities.StudentNote> notes;

        if (request.GroupId.HasValue && request.StudentId.HasValue)
        {
            // Get notes for specific student in group
            notes = await _unitOfWork.StudentNotes.GetNotesByGroupAndStudentAsync(request.GroupId.Value, request.StudentId.Value);
        }
        else if (request.StudentId.HasValue)
        {
            // Get all notes for student
            notes = await _unitOfWork.StudentNotes.GetNotesByStudentIdAsync(request.StudentId.Value);
        }
        else if (request.GroupId.HasValue)
        {
            // Get all notes for group
            notes = await _unitOfWork.StudentNotes.GetNotesByGroupIdAsync(request.GroupId.Value);
        }
        else
        {
            throw new InvalidOperationException("Either StudentId or GroupId must be provided.");
        }

        // Filter notes based on privacy and user role
        var filteredNotes = new List<Domain.Entities.StudentNote>();
        
        foreach (var note in notes)
        {
            // Check if user can access this note
            bool canAccess = false;

            // Notes are private to StudentOffice & assigned Teachers
            if (note.IsPrivate)
            {
                // Check if user is StudentOffice or Teacher in the group
                var userGroupRole = await _unitOfWork.GroupUsers.GetByGroupAndUserAsync(note.GroupId, request.UserId);
                
                canAccess = user.Role == UserRole.MasterAdmin ||
                           user.Role == UserRole.Admin ||
                           user.Role == UserRole.StudentOffice ||
                           (userGroupRole != null && userGroupRole.Role == GroupRole.Teacher) ||
                           note.CreatedById == request.UserId; // Creator can always see their note
            }
            else
            {
                // Non-private notes can be viewed by anyone in the group
                var isUserInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(note.GroupId, request.UserId);
                canAccess = isUserInGroup || user.Role == UserRole.MasterAdmin || user.Role == UserRole.Admin;
            }

            if (canAccess)
            {
                filteredNotes.Add(note);
            }
        }

        var noteDtos = new List<StudentNoteDto>();

        foreach (var note in filteredNotes)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(note.GroupId);
            noteDtos.Add(new StudentNoteDto
            {
                Id = note.Id,
                StudentId = note.StudentId,
                StudentName = await _userRepository.GetUserFullNameAsync(note.StudentId) ?? "Unknown",
                GroupId = note.GroupId,
                GroupName = group?.Name ?? "Unknown",
                Title = note.Title,
                Content = note.Content,
                IsPrivate = note.IsPrivate,
                IsImportant = note.IsImportant,
                CreatedById = note.CreatedById,
                CreatedByName = await _userRepository.GetUserFullNameAsync(note.CreatedById) ?? "Unknown",
                CreatedAt = note.CreatedAt
            });
        }

        return noteDtos.OrderByDescending(n => n.CreatedAt);
    }
}

