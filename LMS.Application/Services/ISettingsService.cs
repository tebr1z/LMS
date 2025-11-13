namespace LMS.Application.Services;

public interface ISettingsService
{
    Task<decimal> GetQuizPassPercentAsync();
    Task<decimal> GetAssignmentPassPercentAsync();
    Task<decimal> GetTeacherAssignmentHighThresholdAsync();
    Task<string> GetLeaderboardWindowAsync(); // "week", "month", "overall"
    Task<decimal> GetSettingDecimalAsync(string key, decimal defaultValue);
    Task<string> GetSettingStringAsync(string key, string defaultValue);
}

