using AutoMapper;
using LMS.Application.DTOs.Courses;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Courses.Queries.GetAllCourses;

public class GetAllCoursesQueryHandler : IRequestHandler<GetAllCoursesQuery, IEnumerable<CourseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetAllCoursesQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CourseDto>> Handle(GetAllCoursesQuery request, CancellationToken cancellationToken)
    {
        List<Course> courses;

        // Role-based filtering
        if (Enum.TryParse<UserRole>(request.UserRole, out var role))
        {
            switch (role)
            {
                case UserRole.MasterAdmin:
                    // MasterAdmin sees all courses
                    courses = await _unitOfWork.Courses.ListAsync();
                    break;

                case UserRole.Admin:
                    // Admin sees courses they created or manage
                    courses = await _unitOfWork.Courses.GetCoursesByCreatorAsync(request.UserId);
                    break;

                case UserRole.Teacher:
                    // Teacher sees courses they created or are enrolled in
                    courses = await _unitOfWork.Courses.GetCoursesForTeacherAsync(request.UserId);
                    break;

                case UserRole.Student:
                    // Student sees courses they are enrolled in
                    courses = await _unitOfWork.Courses.GetCoursesForStudentAsync(request.UserId);
                    break;

                case UserRole.Mentor:
                    // Mentor sees all courses (read-only)
                    courses = await _unitOfWork.Courses.ListAsync();
                    break;

                default:
                    courses = new List<Course>();
                    break;
            }
        }
        else
        {
            // Default: return empty list if role is invalid
            courses = new List<Course>();
        }

        var courseDtos = new List<CourseDto>();

        foreach (var course in courses)
        {
            var dto = _mapper.Map<CourseDto>(course);
            dto.CreatorName = await _userRepository.GetUserFullNameAsync(course.CreatedBy) ?? "Unknown";
            courseDtos.Add(dto);
        }

        return courseDtos;
    }
}

