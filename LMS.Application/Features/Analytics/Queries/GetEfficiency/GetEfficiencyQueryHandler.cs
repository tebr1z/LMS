using LMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Features.Analytics.Queries.GetEfficiency;

public class GetEfficiencyQueryHandler : IRequestHandler<GetEfficiencyQuery, List<EfficiencyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetEfficiencyQueryHandler> _logger;

    public GetEfficiencyQueryHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ILogger<GetEfficiencyQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<List<EfficiencyDto>> Handle(GetEfficiencyQuery request, CancellationToken cancellationToken)
    {
        // Get all submissions with scores and time on page data
        var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var allAssignments = await _unitOfWork.Assignments.ListAsync();
        var allUsers = await _userRepository.ListAsync();

        // Filter submissions to only include those with both score and time data
        var validSubmissions = allSubmissions
            .Where(s => s.Score.HasValue && s.TimeOnPageInSeconds.HasValue && s.TimeOnPageInSeconds.Value > 0)
            .ToList();

        // Apply course instance filter if provided
        if (request.CourseInstanceId.HasValue)
        {
            var courseInstanceAssignments = allAssignments
                .Where(a => a.CourseInstanceId == request.CourseInstanceId.Value)
                .Select(a => a.Id)
                .ToHashSet();

            validSubmissions = validSubmissions
                .Where(s => courseInstanceAssignments.Contains(s.AssignmentId))
                .ToList();
        }

        // Apply group filter if provided
        if (request.GroupId.HasValue)
        {
            var groupAssignments = allAssignments
                .Where(a => a.GroupId == request.GroupId.Value)
                .Select(a => a.Id)
                .ToHashSet();

            validSubmissions = validSubmissions
                .Where(s => groupAssignments.Contains(s.AssignmentId))
                .ToList();
        }

        // Group by student and calculate efficiency metrics
        var efficiencyData = validSubmissions
            .GroupBy(s => s.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                AverageScore = g.Average(s => s.PercentageScore),
                AvgTimeOnPageInSeconds = g.Average(s => (decimal)s.TimeOnPageInSeconds!.Value),
                SubmissionCount = g.Count()
            })
            .Where(x => x.AvgTimeOnPageInSeconds > 0) // Avoid division by zero
            .Select(x => new EfficiencyDto
            {
                StudentId = x.StudentId,
                StudentName = allUsers.FirstOrDefault(u => u.Id == x.StudentId)?.FullName ?? "Unknown",
                AverageScore = Math.Round(x.AverageScore, 2),
                AvgTimeOnPageInMinutes = Math.Round(x.AvgTimeOnPageInSeconds / 60m, 2), // Convert seconds to minutes
                EfficiencyScore = x.AvgTimeOnPageInSeconds > 0
                    ? Math.Round(x.AverageScore / (x.AvgTimeOnPageInSeconds / 60m), 4) // EfficiencyScore = AverageScore / AvgTimeOnPageInMinutes
                    : 0,
                SubmissionCount = x.SubmissionCount
            })
            .OrderByDescending(x => x.EfficiencyScore) // Order by efficiency score descending
            .ToList();

        // Apply TopN limit if provided
        if (request.TopN.HasValue && request.TopN.Value > 0)
        {
            efficiencyData = efficiencyData.Take(request.TopN.Value).ToList();
        }

        return efficiencyData;
    }
}


