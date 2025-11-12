using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAssignmentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Check if course exists
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID {request.CourseId} not found.");
        }

        var assignment = new Assignment
        {
            CourseId = request.CourseId,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Deadline = request.Deadline,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Assignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return assignment.Id;
    }
}

