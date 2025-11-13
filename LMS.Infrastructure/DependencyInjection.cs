using LMS.Application.Interfaces;
using LMS.Application.Interfaces.Admin;
using LMS.Application.Interfaces.Notifications;
using LMS.Application.Interfaces.Payments;
using LMS.Application.Interfaces.Storage;
using LMS.Domain.Entities;
using LMS.Infrastructure.Repositories;
using LMS.Infrastructure.Services;
using LMS.Infrastructure.Services.Notifications;
using LMS.Infrastructure.Services.Payments;
using LMS.Infrastructure.Services.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAdaptiveLearningService, AdaptiveLearningService>();
        services.AddScoped<IAchievementEngine, AchievementEngine>();
        services.AddScoped<LMS.Application.Services.ISettingsService, LMS.Application.Services.SettingsService>();

        // Register File Storage Service
        var storageProvider = configuration["FileStorage:Provider"] ?? "Local";
        if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IFileStorageService, S3FileStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }

        // Register Notification Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRealTimeNotificationService, SignalRNotificationService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Register Payment Service
        var paymentProvider = configuration["Payments:Provider"] ?? "Stripe";
        if (paymentProvider.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IPaymentService, PayPalPaymentService>();
        }
        else
        {
            services.AddScoped<IPaymentService, StripePaymentService>();
        }

        // Register Repositories
        services.AddScoped<IRepository<Course>, EfRepository<Course>>();
        services.AddScoped<IRepository<Enrollment>, EfRepository<Enrollment>>();
        services.AddScoped<IRepository<Message>, EfRepository<Message>>();
        services.AddScoped<IRepository<RefreshToken>, EfRepository<RefreshToken>>();
        
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IAssignmentSubmissionRepository, AssignmentSubmissionRepository>();
        
        // Register UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

