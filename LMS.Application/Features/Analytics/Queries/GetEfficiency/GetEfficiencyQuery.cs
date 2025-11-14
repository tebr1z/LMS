using MediatR;

namespace LMS.Application.Features.Analytics.Queries.GetEfficiency;

public class GetEfficiencyQuery : IRequest<List<EfficiencyDto>>
{
    public int? CourseInstanceId { get; set; } // Optional: filter by course instance
    public int? GroupId { get; set; } // Optional: filter by group
    public int? TopN { get; set; } // Optional: limit number of results
}

public class EfficiencyDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal AverageScore { get; set; } // Average percentage score (0-100)
    public decimal AvgTimeOnPageInMinutes { get; set; } // Average time spent on assignments in minutes
    public decimal EfficiencyScore { get; set; } // EfficiencyScore = AverageScore / AvgTimeOnPageInMinutes
    public int SubmissionCount { get; set; } // Number of submissions with both score and time data
}


