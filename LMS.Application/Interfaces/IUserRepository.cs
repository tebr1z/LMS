namespace LMS.Application.Interfaces;

public interface IUserRepository
{
    Task<string?> GetUserFullNameAsync(int userId);
}

