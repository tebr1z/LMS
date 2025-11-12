using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Repositories;
using LMS.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register Services
        services.AddScoped<IAuthService, AuthService>();

        // Register Repositories
        services.AddScoped<IRepository<Course>, EfRepository<Course>>();
        services.AddScoped<IRepository<Enrollment>, EfRepository<Enrollment>>();
        services.AddScoped<IRepository<Message>, EfRepository<Message>>();
        services.AddScoped<IRepository<RefreshToken>, EfRepository<RefreshToken>>();
        
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Register UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

