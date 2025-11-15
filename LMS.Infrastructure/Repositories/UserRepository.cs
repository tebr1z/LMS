using LMS.Application.Interfaces;
using LMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetUserFullNameAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.FullName;
    }

    public async Task<UserDetails?> GetUserByIdAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return null;

        return new UserDetails
        {
            Id = user.Id,
            FullName = user.FullName,
            Role = user.Role,
            Email = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null;
    }

    public async Task<List<UserDetails>> ListAsync()
    {
        var users = _userManager.Users.ToList();
        return users.Select(user => new UserDetails
        {
            Id = user.Id,
            FullName = user.FullName,
            Role = user.Role,
            Email = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt
        }).ToList();
    }

    public async Task<List<UserDetails>> GetUsersByIdsAsync(IEnumerable<int> userIds)
    {
        var ids = userIds?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            return new List<UserDetails>();
        }

        var users = await _userManager.Users
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();

        return users.Select(user => new UserDetails
        {
            Id = user.Id,
            FullName = user.FullName,
            Role = user.Role,
            Email = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt
        }).ToList();
    }
}

