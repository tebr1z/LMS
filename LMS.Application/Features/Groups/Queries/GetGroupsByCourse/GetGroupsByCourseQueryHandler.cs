using AutoMapper;
using LMS.Application.DTOs.Groups;
using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Groups.Queries.GetGroupsByCourse;

public class GetGroupsByCourseQueryHandler : IRequestHandler<GetGroupsByCourseQuery, IEnumerable<GroupDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetGroupsByCourseQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<GroupDto>> Handle(GetGroupsByCourseQuery request, CancellationToken cancellationToken)
    {
        // Check if course exists
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID {request.CourseId} not found.");
        }

        // Get groups based on role
        List<Group> groups;
        var allGroupsForCourse = await _unitOfWork.Groups.GetGroupsByCourseIdAsync(request.CourseId);

        // Filter groups based on user role
        if (request.UserRole == "MasterAdmin" || request.UserRole == "Mentor")
        {
            // MasterAdmin and Mentor see all groups
            groups = allGroupsForCourse;
        }
        else if (request.UserRole == "Admin")
        {
            // Admin sees groups they created
            groups = allGroupsForCourse.Where(g => g.CreatedBy == request.UserId).ToList();
        }
        else if (request.UserRole == "Teacher")
        {
            // Teacher sees groups they are assigned to
            groups = allGroupsForCourse.Where(g => g.GroupUsers.Any(gu => gu.UserId == request.UserId)).ToList();
        }
        else if (request.UserRole == "Student")
        {
            // Student sees groups they belong to
            groups = allGroupsForCourse.Where(g => g.GroupUsers.Any(gu => gu.UserId == request.UserId)).ToList();
        }
        else
        {
            groups = new List<Group>();
        }

        var groupDtos = new List<GroupDto>();

        foreach (var group in groups)
        {
            var groupDto = _mapper.Map<GroupDto>(group);

            // Map courses
            foreach (var courseGroup in group.CourseGroups)
            {
                var courseEntity = await _unitOfWork.Courses.GetByIdAsync(courseGroup.CourseId);
                if (courseEntity != null)
                {
                    groupDto.Courses.Add(new CourseGroupDto
                    {
                        Id = courseGroup.Id,
                        CourseId = courseEntity.Id,
                        CourseTitle = courseEntity.Title
                    });
                }
            }

            // Map users
            foreach (var groupUser in group.GroupUsers)
            {
                var userName = await _userRepository.GetUserFullNameAsync(groupUser.UserId);
                groupDto.Users.Add(new GroupUserDto
                {
                    Id = groupUser.Id,
                    UserId = groupUser.UserId,
                    UserName = userName ?? "Unknown",
                    Role = groupUser.Role
                });
            }

            groupDtos.Add(groupDto);
        }

        return groupDtos;
    }
}

