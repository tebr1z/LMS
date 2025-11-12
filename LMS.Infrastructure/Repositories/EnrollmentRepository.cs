using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class EnrollmentRepository : EfRepository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<bool> IsUserEnrolledAsync(int userId, int courseId)
    {
        return await _dbSet
            .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
    }

    public async Task<List<Enrollment>> GetEnrollmentsByUserAsync(int userId)
    {
        return await _dbSet
            .Where(e => e.UserId == userId)
            .Include(e => e.Course)
            .ToListAsync();
    }

    public async Task<List<Enrollment>> GetEnrollmentsByCourseAsync(int courseId)
    {
        return await _dbSet
            .Where(e => e.CourseId == courseId)
            .ToListAsync();
    }
}

