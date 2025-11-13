namespace LMS.Domain.Entities;

public class StudentStats : BaseEntity
{
    public int StudentId { get; set; }
    public int? CourseInstanceId { get; set; } // Optional: stats per course instance
    public int TotalPoints { get; set; } = 0; // Total points earned
    public int TotalPossiblePoints { get; set; } = 0; // Total possible points
    public decimal AveragePercent { get; set; } = 0; // Average percentage score
    public int AssignmentsPassedCount { get; set; } = 0; // Number of passed assignments
    public int AssignmentsCompletedCount { get; set; } = 0; // Total assignments completed
    public int QuizzesPassedCount { get; set; } = 0; // Number of passed quizzes
    public int QuizzesAttemptedCount { get; set; } = 0; // Total quizzes attempted
    public DateTime? LastActivity { get; set; } // Last activity timestamp

    // Navigation properties
}

