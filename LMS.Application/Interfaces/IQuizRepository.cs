using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IQuizRepository : IRepository<Quiz>
{
    Task<Quiz?> GetByAssignmentIdAsync(int assignmentId);
    Task<Quiz?> GetQuizWithQuestionsAsync(int quizId);
    Task<List<Quiz>> GetQuizzesByAssignmentIdsAsync(List<int> assignmentIds);
}

