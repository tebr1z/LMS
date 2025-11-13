using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class QuizRepository : EfRepository<Quiz>, IQuizRepository
{
    public QuizRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<Quiz?> GetByAssignmentIdAsync(int assignmentId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(q => q.AssignmentId == assignmentId);
    }

    public async Task<Quiz?> GetQuizWithQuestionsAsync(int quizId)
    {
        return await _dbSet
            .Include(q => q.Questions.OrderBy(qq => qq.Order))
            .Include(q => q.Assignment)
            .FirstOrDefaultAsync(q => q.Id == quizId);
    }

    public async Task<List<Quiz>> GetQuizzesByAssignmentIdsAsync(List<int> assignmentIds)
    {
        return await _dbSet
            .Where(q => assignmentIds.Contains(q.AssignmentId))
            .Include(q => q.Questions)
            .ToListAsync();
    }
}

