using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IQuizSessionRepository : IRepository<QuizSession>
{
    Task<QuizSession?> GetActiveSessionByStudentAndQuizAsync(int studentId, int quizId);
    Task<List<QuizSession>> GetSessionsByQuizIdAsync(int quizId);
    Task<List<QuizSession>> GetCompletedSessionsByQuizIdAsync(int quizId);
    Task<List<QuizSession>> GetSessionsByStudentIdAsync(int studentId);
    Task<List<QuizSession>> GetSessionsByAssignmentIdAsync(int assignmentId);
    Task<QuizSession?> GetSessionWithResponsesAsync(int sessionId);
}

