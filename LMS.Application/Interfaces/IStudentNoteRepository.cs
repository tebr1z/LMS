using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IStudentNoteRepository : IRepository<StudentNote>
{
    Task<List<StudentNote>> GetNotesByStudentIdAsync(int studentId);
    Task<List<StudentNote>> GetNotesByGroupIdAsync(int groupId);
    Task<List<StudentNote>> GetNotesByGroupAndStudentAsync(int groupId, int studentId);
    Task<List<StudentNote>> GetNotesAccessibleByUserAsync(int userId, int groupId, int? studentId = null);
}

