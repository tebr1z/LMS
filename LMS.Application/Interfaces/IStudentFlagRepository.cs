using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IStudentFlagRepository : IRepository<StudentFlag>
{
    Task<List<StudentFlag>> GetFlagsByStudentIdAsync(int studentId);
    Task<List<StudentFlag>> GetActiveFlagsAsync(bool isResolved = false);
    Task<List<StudentFlag>> GetFlagsByCreatedByIdAsync(int createdById);
}

