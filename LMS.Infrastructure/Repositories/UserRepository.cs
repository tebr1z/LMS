using LMS.Application.Interfaces;
using LMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

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
            Email = user.Email ?? string.Empty
        };
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null;
    }
}

