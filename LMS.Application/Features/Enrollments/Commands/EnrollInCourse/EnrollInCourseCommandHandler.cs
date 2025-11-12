using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.Enrollments.Commands.EnrollInCourse;

public class EnrollInCourseCommandHandler : IRequestHandler<EnrollInCourseCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public EnrollInCourseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(EnrollInCourseCommand request, CancellationToken cancellationToken)
    {
        // Check if user is already enrolled
        var isEnrolled = await _unitOfWork.Enrollments.IsUserEnrolledAsync(
            request.UserId, 
            request.CourseId);

        if (isEnrolled)
        {
            throw new InvalidOperationException("User is already enrolled in this course.");
        }

        // Check if course exists
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            throw new InvalidOperationException("Course not found.");
        }

        var enrollment = new Enrollment
        {
            UserId = request.UserId,
            CourseId = request.CourseId,
            EnrolledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Enrollments.AddAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();

        return enrollment.Id;
    }
}

