using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class QuizTelemetryRepository : EfRepository<QuizTelemetry>, IQuizTelemetryRepository
{
    public QuizTelemetryRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<QuizTelemetry>> GetTelemetryByStudentAndQuizAsync(int studentId, int quizId)
    {
        return await _dbSet
            .Where(qt => qt.StudentId == studentId && qt.QuizId == quizId)
            .OrderBy(qt => qt.Timestamp)
            .ToListAsync();
    }

    public async Task<List<QuizTelemetry>> GetTelemetryBySessionIdAsync(string sessionId)
    {
        return await _dbSet
            .Where(qt => qt.SessionId == sessionId)
            .OrderBy(qt => qt.Timestamp)
            .ToListAsync();
    }

    public async Task<List<QuizTelemetry>> GetTelemetryByQuizSessionAsync(int quizSessionId)
    {
        return await _dbSet
            .Where(qt => qt.QuizSessionId == quizSessionId)
            .OrderBy(qt => qt.Timestamp)
            .ToListAsync();
    }
}

