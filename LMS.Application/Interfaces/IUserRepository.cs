using LMS.Domain.Enums;

namespace LMS.Application.Interfaces;

public interface IUserRepository
{
    Task<string?> GetUserFullNameAsync(int userId);
    Task<UserDetails?> GetUserByIdAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
    Task<List<UserDetails>> ListAsync(); // Get all users
    Task<List<UserDetails>> GetUsersByIdsAsync(IEnumerable<int> userIds);
}

public class UserDetails
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

