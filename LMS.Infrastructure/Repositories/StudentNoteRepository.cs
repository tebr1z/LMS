using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class StudentNoteRepository : EfRepository<StudentNote>, IStudentNoteRepository
{
    public StudentNoteRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<StudentNote>> GetNotesByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(n => n.StudentId == studentId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<StudentNote>> GetNotesByGroupIdAsync(int groupId)
    {
        return await _dbSet
            .Where(n => n.GroupId == groupId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<StudentNote>> GetNotesByGroupAndStudentAsync(int groupId, int studentId)
    {
        return await _dbSet
            .Where(n => n.GroupId == groupId && n.StudentId == studentId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<StudentNote>> GetNotesAccessibleByUserAsync(int userId, int groupId, int? studentId = null)
    {
        // Get user's global role and group role
        // For now, we'll check if notes are private and user is Teacher/StudentOffice
        // In a real implementation, you'd check user's role in the group
        
        var query = _dbSet.Where(n => n.GroupId == groupId);
        
        if (studentId.HasValue)
        {
            query = query.Where(n => n.StudentId == studentId.Value);
        }

        var notes = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();

        // Filter notes based on privacy and user role
        // Private notes are only visible to StudentOffice & assigned Teachers
        // This will be refined in the query handler with proper role checks
        return notes;
    }
}

