using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class QuizSessionRepository : EfRepository<QuizSession>, IQuizSessionRepository
{
    public QuizSessionRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<QuizSession?> GetActiveSessionByStudentAndQuizAsync(int studentId, int quizId)
    {
        return await _dbSet
            .Where(qs => qs.StudentId == studentId && qs.QuizId == quizId && !qs.IsCompleted)
            .OrderByDescending(qs => qs.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<QuizSession>> GetSessionsByQuizIdAsync(int quizId)
    {
        return await _dbSet
            .Where(qs => qs.QuizId == quizId)
            .Include(qs => qs.Responses)
            .ThenInclude(qr => qr.Question)
            .OrderByDescending(qs => qs.StartedAt)
            .ToListAsync();
    }

    public async Task<List<QuizSession>> GetCompletedSessionsByQuizIdAsync(int quizId)
    {
        return await _dbSet
            .Where(qs => qs.QuizId == quizId && qs.IsCompleted)
            .Include(qs => qs.Responses)
            .ThenInclude(qr => qr.Question)
            .OrderByDescending(qs => qs.EndedAt)
            .ToListAsync();
    }

    public async Task<List<QuizSession>> GetSessionsByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(qs => qs.StudentId == studentId)
            .Include(qs => qs.Quiz)
            .ThenInclude(q => q!.Assignment)
            .OrderByDescending(qs => qs.StartedAt)
            .ToListAsync();
    }

    public async Task<List<QuizSession>> GetSessionsByAssignmentIdAsync(int assignmentId)
    {
        return await _dbSet
            .Where(qs => qs.AssignmentId == assignmentId)
            .Include(qs => qs.Responses)
            .ThenInclude(qr => qr.Question)
            .Include(qs => qs.Quiz)
            .OrderByDescending(qs => qs.StartedAt)
            .ToListAsync();
    }

    public async Task<QuizSession?> GetSessionWithResponsesAsync(int sessionId)
    {
        return await _dbSet
            .Include(qs => qs.Responses)
            .ThenInclude(qr => qr.Question)
            .Include(qs => qs.Quiz)
            .ThenInclude(q => q!.Questions)
            .Include(qs => qs.Assignment)
            .FirstOrDefaultAsync(qs => qs.Id == sessionId);
    }
}

