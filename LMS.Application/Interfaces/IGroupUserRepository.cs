using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IGroupUserRepository : IRepository<GroupUser>
{
    Task<List<GroupUser>> GetGroupUsersByGroupIdAsync(int groupId);
    Task<GroupUser?> GetByGroupAndUserAsync(int groupId, int userId);
    Task<bool> IsUserAssignedToGroupAsync(int groupId, int userId);
}

