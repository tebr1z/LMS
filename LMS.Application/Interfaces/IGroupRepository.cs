using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IGroupRepository : IRepository<Group>
{
    Task<Group?> GetGroupWithDetailsAsync(int groupId);
    Task<List<Group>> GetGroupsByCourseIdAsync(int courseId);
    Task<bool> IsUserInGroupAsync(int groupId, int userId);
    Task<bool> IsCourseInGroupAsync(int groupId, int courseId);
}

