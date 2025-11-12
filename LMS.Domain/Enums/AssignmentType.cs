namespace LMS.Domain.Enums;

public enum AssignmentType
{
    ReadingMaterial = 1,  // No score
    FileSubmission = 2,    // PDF, video, image
    VideoSubmission = 3,   // Video submission
    ImageSubmission = 4,   // Image submission
    Test = 5,              // Quiz
    Project = 6            // Graded manually
}

