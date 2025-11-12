using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class CourseRepository : EfRepository<Course>, ICourseRepository
{
    public CourseRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<Course>> GetCoursesByCreatorAsync(int creatorId)
    {
        return await _dbSet
            .Where(c => c.CreatedBy == creatorId)
            .ToListAsync();
    }

    public async Task<Course?> GetCourseWithEnrollmentsAsync(int courseId)
    {
        return await _dbSet
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public override async Task<List<Course>> ListAsync()
    {
        return await _dbSet
            .Include(c => c.Enrollments)
            .ToListAsync();
    }

    public async Task<List<Course>> GetCoursesForTeacherAsync(int teacherId)
    {
        // Teacher sees courses they created OR courses they are enrolled in
        return await _dbSet
            .Where(c => c.CreatedBy == teacherId || 
                       c.Enrollments.Any(e => e.UserId == teacherId))
            .Include(c => c.Enrollments)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<Course>> GetCoursesForStudentAsync(int studentId)
    {
        // Student sees courses they are enrolled in
        return await _dbSet
            .Where(c => c.Enrollments.Any(e => e.UserId == studentId))
            .Include(c => c.Enrollments)
            .ToListAsync();
    }
}

