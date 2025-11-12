using AutoMapper;
using LMS.Application.DTOs.Assignments;
using LMS.Application.DTOs.Courses;
using LMS.Application.DTOs.Groups;
using LMS.Domain.Entities;

namespace LMS.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Course, CourseDto>()
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentCount, opt => opt.MapFrom(src => src.Enrollments.Count));

        CreateMap<Group, GroupDto>()
            .ForMember(dest => dest.Courses, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore());

        CreateMap<Assignment, AssignmentDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByName, opt => opt.Ignore())
            .ForMember(dest => dest.TypeName, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeadlinePassed, opt => opt.Ignore())
            .ForMember(dest => dest.SubmissionCount, opt => opt.Ignore());

        CreateMap<AssignmentSubmission, AssignmentSubmissionDto>()
            .ForMember(dest => dest.AssignmentTitle, opt => opt.Ignore())
            .ForMember(dest => dest.StudentName, opt => opt.Ignore())
            .ForMember(dest => dest.EvaluatedByName, opt => opt.Ignore());
    }
}

