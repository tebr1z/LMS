using AutoMapper;
using LMS.Application.DTOs.Courses;
using LMS.Application.Interfaces;
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
        var courses = await _unitOfWork.Courses.ListAsync();
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

