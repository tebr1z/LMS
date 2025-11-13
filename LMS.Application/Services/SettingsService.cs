using LMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LMS.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public SettingsService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<decimal> GetQuizPassPercentAsync()
    {
        // Try database first, fall back to appsettings.json
        var dbValue = await _unitOfWork.SystemSettings.GetValueByKeyAsync("QuizPassPercent");
        if (!string.IsNullOrEmpty(dbValue) && decimal.TryParse(dbValue, out var dbDecimal))
        {
            return dbDecimal;
        }

        // Fall back to appsettings.json
        return _configuration.GetValue<decimal>("Settings:QuizPassPercent", 80);
    }

    public async Task<decimal> GetAssignmentPassPercentAsync()
    {
        var dbValue = await _unitOfWork.SystemSettings.GetValueByKeyAsync("AssignmentPassPercent");
        if (!string.IsNullOrEmpty(dbValue) && decimal.TryParse(dbValue, out var dbDecimal))
        {
            return dbDecimal;
        }

        return _configuration.GetValue<decimal>("Settings:AssignmentPassPercent", 80);
    }

    public async Task<decimal> GetTeacherAssignmentHighThresholdAsync()
    {
        var dbValue = await _unitOfWork.SystemSettings.GetValueByKeyAsync("TeacherAssignmentHighThreshold");
        if (!string.IsNullOrEmpty(dbValue) && decimal.TryParse(dbValue, out var dbDecimal))
        {
            return dbDecimal;
        }

        return _configuration.GetValue<decimal>("Settings:TeacherAssignmentHighThreshold", 90);
    }

    public async Task<string> GetLeaderboardWindowAsync()
    {
        var dbValue = await _unitOfWork.SystemSettings.GetValueByKeyAsync("LeaderboardWindow");
        if (!string.IsNullOrEmpty(dbValue))
        {
            return dbValue;
        }

        return _configuration.GetValue<string>("Settings:LeaderboardWindow", "overall") ?? "overall";
    }

    public async Task<decimal> GetSettingDecimalAsync(string key, decimal defaultValue)
    {
        var dbValue = await _unitOfWork.SystemSettings.GetValueByKeyAsync(key);
        if (!string.IsNullOrEmpty(dbValue) && decimal.TryParse(dbValue, out var dbDecimal))
        {
            return dbDecimal;
        }

        return _configuration.GetValue<decimal>($"Settings:{key}", defaultValue);
    }

    public async Task<string> GetSettingStringAsync(string key, string defaultValue)
    {
        var dbValue = await _unitOfWork.SystemSettings.GetValueByKeyAsync(key);
        if (!string.IsNullOrEmpty(dbValue))
        {
            return dbValue;
        }

        return _configuration.GetValue<string>($"Settings:{key}", defaultValue) ?? defaultValue;
    }
}

