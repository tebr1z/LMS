using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class SystemSettingsRepository : EfRepository<SystemSettings>, ISystemSettingsRepository
{
    public SystemSettingsRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<SystemSettings?> GetByKeyAsync(string key)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<string?> GetValueByKeyAsync(string key)
    {
        var setting = await GetByKeyAsync(key);
        return setting?.Value;
    }

    public async Task<List<SystemSettings>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(s => s.Category == category)
            .OrderBy(s => s.Key)
            .ToListAsync();
    }
}

