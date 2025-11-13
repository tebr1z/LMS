using AutoMapper;
using LMS.Application.DTOs.Groups;
using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Groups.Queries.GetGroupDetails;

public class GetGroupDetailsQueryHandler : IRequestHandler<GetGroupDetailsQuery, GroupDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetGroupDetailsQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<GroupDto> Handle(GetGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetGroupWithDetailsAsync(request.GroupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found.");
        }

        // Check access based on role
        if (request.UserRole != "MasterAdmin" && request.UserRole != "Mentor")
        {
            if (request.UserRole == "Admin")
            {
                // Admin can only see groups they created
                if (group.CreatedById != request.UserId)
                {
                    throw new UnauthorizedAccessException("You do not have access to this group.");
                }
            }
            else if (request.UserRole == "Teacher" || request.UserRole == "Student")
            {
                // Teacher and Student can only see groups they belong to
                if (!group.GroupUsers.Any(gu => gu.UserId == request.UserId))
                {
                    throw new UnauthorizedAccessException("You do not have access to this group.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("You do not have access to this group.");
            }
        }

        var groupDto = _mapper.Map<GroupDto>(group);

        // Map courses (CoursePrepared linked to this group)
        foreach (var courseGroup in group.CourseGroups)
        {
            var coursePrepared = await _unitOfWork.CoursePrepareds.GetByIdAsync(courseGroup.CoursePreparedId);
            if (coursePrepared != null)
            {
                groupDto.Courses.Add(new CourseGroupDto
                {
                    Id = courseGroup.Id,
                    CourseId = coursePrepared.Id,
                    CourseTitle = coursePrepared.Title
                });
            }
        }

        // Map users with their roles
        foreach (var groupUser in group.GroupUsers)
        {
            var userName = await _userRepository.GetUserFullNameAsync(groupUser.UserId);
            groupDto.Users.Add(new GroupUserDto
            {
                Id = groupUser.Id,
                UserId = groupUser.UserId,
                UserName = userName ?? "Unknown",
                Role = groupUser.Role.ToString()
            });
        }

        return groupDto;
    }
}

