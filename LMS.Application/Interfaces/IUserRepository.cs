using LMS.Domain.Enums;

namespace LMS.Application.Interfaces;

public interface IUserRepository
{
    Task<string?> GetUserFullNameAsync(int userId);
    Task<UserDetails?> GetUserByIdAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
}

public class UserDetails
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Email { get; set; } = string.Empty;
}

