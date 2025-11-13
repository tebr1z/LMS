using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface ISystemSettingsRepository : IRepository<SystemSettings>
{
    Task<SystemSettings?> GetByKeyAsync(string key);
    Task<string?> GetValueByKeyAsync(string key);
    Task<List<SystemSettings>> GetByCategoryAsync(string category);
}

