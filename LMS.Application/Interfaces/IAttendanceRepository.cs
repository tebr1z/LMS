using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<List<Attendance>> GetAttendancesByGroupIdAsync(int groupId);
    Task<List<Attendance>> GetAttendancesByGroupAndDateAsync(int groupId, DateTime date);
    Task<List<Attendance>> GetAttendancesByStudentIdAsync(int studentId);
    Task<Attendance?> GetAttendanceByGroupStudentAndDateAsync(int groupId, int studentId, DateTime date);
}

