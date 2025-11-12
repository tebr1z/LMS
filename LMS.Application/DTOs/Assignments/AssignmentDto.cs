using LMS.Domain.Enums;

namespace LMS.Application.DTOs.Assignments;

public class AssignmentDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AssignmentType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public bool IsDeadlinePassed { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int SubmissionCount { get; set; }
}

public class AssignmentSubmissionDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? AnswerText { get; set; }
    public DateTime SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public int? EvaluatedBy { get; set; }
    public string? EvaluatedByName { get; set; }
}

