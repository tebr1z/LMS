using LMS.Application.Interfaces;
using LMS.Application.Interfaces.Notifications;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service implementation for live sessions with Jitsi integration
/// </summary>
public class LiveSessionService : ILiveSessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LiveSessionService> _logger;
    private readonly INotificationService? _notificationService;

    public LiveSessionService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<LiveSessionService> logger,
        INotificationService? notificationService = null)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
        _notificationService = notificationService; // Optional dependency
    }

    public async Task<string> StartSessionAsync(int groupId, int teacherId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if there's already an active session
            var existingSession = await _unitOfWork.LiveSessions.GetActiveSessionByGroupIdAsync(groupId, cancellationToken);
            if (existingSession != null)
            {
                _logger.LogInformation("Active session already exists for group {GroupId}: {SessionId}", groupId, existingSession.Id);
                return existingSession.SessionUrl;
            }

            // Generate unique room name
            var roomName = $"group-{groupId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            // Get Jitsi server URL from configuration (default: meet.jit.si)
            var jitsiServerUrl = _configuration["Jitsi:ServerUrl"] ?? "https://meet.jit.si";

            // Generate JWT token for moderator (teacher)
            var jwtToken = GenerateJwtToken(roomName, $"teacher-{teacherId}", "teacher", isModerator: true);

            // Create session URL with JWT
            var sessionUrl = $"{jitsiServerUrl}/{roomName}?jwt={jwtToken}";

            // Create live session entity
            var liveSession = new LiveSession
            {
                GroupId = groupId,
                TeacherId = teacherId,
                StartTime = DateTime.UtcNow,
                SessionUrl = sessionUrl,
                JwtToken = jwtToken,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.LiveSessions.AddAsync(liveSession);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Started live session {SessionId} for group {GroupId} by teacher {TeacherId}",
                liveSession.Id, groupId, teacherId);

            // Send notification to all students in the group about the new live session
            try
            {
                await NotifyGroupAboutLiveSessionAsync(groupId, liveSession.Id, teacherId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notifications for live session {SessionId}", liveSession.Id);
                // Don't fail the entire operation if notification fails
            }

            return sessionUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting live session for group {GroupId}", groupId);
            throw;
        }
    }

    public async Task EndSessionAsync(int sessionId, int teacherId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _unitOfWork.LiveSessions.GetSessionWithDetailsAsync(sessionId);
            if (session == null)
            {
                throw new InvalidOperationException($"Live session {sessionId} not found.");
            }

            // Verify teacher owns this session
            if (session.TeacherId != teacherId)
            {
                throw new UnauthorizedAccessException("Only the session creator can end the session.");
            }

            // Check if already ended
            if (session.EndTime.HasValue)
            {
                _logger.LogInformation("Session {SessionId} already ended", sessionId);
                return;
            }

            session.EndTime = DateTime.UtcNow;
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.LiveSessions.UpdateAsync(session);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ended live session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending live session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<string?> GetCurrentSessionUrlAsync(int groupId, int userId, string userRole, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _unitOfWork.LiveSessions.GetActiveSessionByGroupIdAsync(groupId, cancellationToken);
            if (session == null)
            {
                return null;
            }

            // Verify user has access to this group
            // Get group users to verify membership
            var groupUsers = await _unitOfWork.GroupUsers.GetGroupUsersByGroupIdAsync(groupId);
            var hasAccess = groupUsers.Any(gu => gu.UserId == userId) || 
                           userRole == UserRole.Admin.ToString() || 
                           userRole == UserRole.MasterAdmin.ToString();

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You do not have access to this group's live session.");
            }

            // Generate JWT token for the user (student gets participant role, teacher gets moderator)
            var isModerator = session.TeacherId == userId || userRole == UserRole.Teacher.ToString() || 
                            userRole == UserRole.Admin.ToString() || 
                            userRole == UserRole.MasterAdmin.ToString();

            var roomName = ExtractRoomNameFromUrl(session.SessionUrl);
            var jwtToken = GenerateJwtToken(roomName, $"user-{userId}", userRole, isModerator);

            // Return URL with user-specific JWT
            var jitsiServerUrl = _configuration["Jitsi:ServerUrl"] ?? "https://meet.jit.si";
            return $"{jitsiServerUrl}/{roomName}?jwt={jwtToken}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current session URL for group {GroupId}", groupId);
            throw;
        }
    }

    public string GenerateJwtToken(string roomName, string userName, string userRole, bool isModerator)
    {
        try
        {
            // Get Jitsi secret key from configuration
            var secretKey = _configuration["Jitsi:SecretKey"] ?? throw new InvalidOperationException("Jitsi SecretKey is not configured");
            var appId = _configuration["Jitsi:AppId"] ?? "LMS";
            var issuer = _configuration["Jitsi:Issuer"] ?? "LMS";
            var audience = _configuration["Jitsi:Audience"] ?? "jitsi";

            // Create claims
            var claims = new List<Claim>
            {
                new Claim("iss", issuer),
                new Claim("aud", audience),
                new Claim("sub", appId),
                new Claim("room", roomName),
                new Claim("name", userName),
                new Claim("email", $"{userName}@lms.local"),
                new Claim("moderator", isModerator.ToString().ToLowerInvariant()),
                new Claim("context", JsonSerializer.Serialize(new
                {
                    user = new
                    {
                        id = userName,
                        name = userName,
                        moderator = isModerator
                    }
                }))
            };

            // Create signing key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4), // Jitsi tokens typically valid for 4 hours
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for room {RoomName}", roomName);
            throw;
        }
    }

    private string ExtractRoomNameFromUrl(string sessionUrl)
    {
        // Extract room name from URL like: https://meet.jit.si/group-123-20240101120000?jwt=...
        var uri = new Uri(sessionUrl);
        var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return pathSegments.Length > 0 ? pathSegments[pathSegments.Length - 1] : $"room-{Guid.NewGuid()}";
    }

    private async Task NotifyGroupAboutLiveSessionAsync(int groupId, int sessionId, int teacherId, CancellationToken cancellationToken)
    {
        if (_notificationService == null)
        {
            return; // Notification service not available
        }

        try
        {
            // Get all students in the group
            var groupUsers = await _unitOfWork.GroupUsers.GetGroupUsersByGroupIdAsync(groupId);
            var studentIds = groupUsers
                .Where(gu => gu.Role == GroupRole.Student)
                .Select(gu => gu.UserId)
                .ToList();

            if (!studentIds.Any())
            {
                return;
            }

            // Get teacher name and group name
            var teacher = await _userRepository.GetUserByIdAsync(teacherId);
            var teacherName = teacher?.FullName ?? "Teacher";

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            var groupName = group?.Name ?? $"Group {groupId}";

            // Get all students with their emails
            var allUsers = await _userRepository.ListAsync();
            var students = allUsers.Where(u => studentIds.Contains(u.Id)).ToList();

            // Create notification for each student
            var notificationData = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["groupId"] = groupId,
                ["groupName"] = groupName,
                ["teacherId"] = teacherId,
                ["teacherName"] = teacherName,
                ["type"] = "live_session_started"
            };

            foreach (var student in students)
            {
                var notificationMessage = new NotificationMessage
                {
                    Title = $"Canlı Ders Başladı - {groupName}",
                    Message = $"{teacherName} canlı dersi başlattı. Hemen katıl!",
                    Type = "info",
                    Data = notificationData
                };

                await _notificationService.SendNotificationAsync(
                    userId: student.Id,
                    email: student.Email,
                    notification: notificationMessage,
                    sendEmail: false, // Only in-app notification
                    sendRealTime: true,
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation(
                "Sent live session notifications to {Count} students in group {GroupId}",
                studentIds.Count, groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying group {GroupId} about live session", groupId);
            throw;
        }
    }
}

