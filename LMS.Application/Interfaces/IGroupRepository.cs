using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IGroupRepository : IRepository<Group>
{
    Task<Group?> GetGroupWithDetailsAsync(int groupId);
    Task<List<Group>> GetGroupsByCourseIdAsync(int coursePreparedId);
    Task<bool> IsUserInGroupAsync(int groupId, int userId);
    Task<bool> IsCourseInGroupAsync(int groupId, int coursePreparedId);
    Task<List<Group>> GetGroupsForUserAsync(int userId, string userRole);
    Task<List<Group>> GetGroupsForStudentAsync(int studentId);
    Task<List<Group>> GetGroupsForTeacherAsync(int teacherId);
    Task<List<Group>> GetGroupsForAdminAsync(int adminId);
}

