using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IQuizTelemetryRepository : IRepository<QuizTelemetry>
{
    Task<List<QuizTelemetry>> GetTelemetryByStudentAndQuizAsync(int studentId, int quizId);
    Task<List<QuizTelemetry>> GetTelemetryBySessionIdAsync(string sessionId);
    Task<List<QuizTelemetry>> GetTelemetryByQuizSessionAsync(int quizSessionId);
}

