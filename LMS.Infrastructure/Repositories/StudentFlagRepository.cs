using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class StudentFlagRepository : EfRepository<StudentFlag>, IStudentFlagRepository
{
    public StudentFlagRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<StudentFlag>> GetFlagsByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(sf => sf.StudentId == studentId)
            .OrderByDescending(sf => sf.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<StudentFlag>> GetActiveFlagsAsync(bool isResolved = false)
    {
        return await _dbSet
            .Where(sf => sf.IsResolved == isResolved)
            .OrderByDescending(sf => sf.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<StudentFlag>> GetFlagsByCreatedByIdAsync(int createdById)
    {
        return await _dbSet
            .Where(sf => sf.CreatedById == createdById)
            .OrderByDescending(sf => sf.CreatedAt)
            .ToListAsync();
    }
}

