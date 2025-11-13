using System.Text;
using LMS.Application;
using LMS.Infrastructure;
using LMS.Infrastructure.Data;
using LMS.API.Hubs;
using LMS.API.Extensions;
using LMS.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LMS API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Database (EF Core with MySQL) and Identity
builder.Services.AddDatabaseContext(builder.Configuration);

// Configure Application Services (MediatR, AutoMapper, FluentValidation)
builder.Services.AddApplication();

// Configure Infrastructure Services
builder.Services.AddInfrastructure(builder.Configuration);

// Register background services for automated notifications
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.OverdueInvoiceNotificationService>();
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.AssignmentDeadlineNotificationService>();
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.StudentThresholdNotificationService>();
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.AdaptiveLearningBackgroundService>();
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.AchievementBackgroundService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Configure JWT authentication for SignalR
    // Token can be passed in query string (access_token) or header (Authorization: Bearer <token>)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // If the request is for a SignalR hub and token is in query string
            // Support all SignalR hubs: /chatHub, /quizHub, /notificationHub, etc.
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/chatHub") || 
                 path.StartsWithSegments("/quizHub") || 
                 path.StartsWithSegments("/notificationHub") ||
                 path.StartsWithSegments("/notificationsHub") ||
                 path.StartsWithSegments("/hubs")))
            {
                context.Token = accessToken;
            }

            // Token can also be passed in Authorization header (default behavior)
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization with role-based policies
builder.Services.AddAuthorization(options =>
{
    // Define role-based policies with hierarchical access
    // RequireRole allows access if user has any of the specified roles
    options.AddPolicy("MasterAdmin", policy => policy.RequireRole("MasterAdmin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin", "MasterAdmin"));
    options.AddPolicy("Teacher", policy => policy.RequireRole("Teacher", "Admin", "MasterAdmin"));
    options.AddPolicy("Mentor", policy => policy.RequireRole("Mentor", "Teacher", "Admin", "MasterAdmin")); // Note: Mentor will have read-only in business logic
    options.AddPolicy("Student", policy => policy.RequireRole("Student", "StudentOffice", "Mentor", "Teacher", "Admin", "MasterAdmin"));
    options.AddPolicy("Finance", policy => policy.RequireRole("Finance", "Admin", "MasterAdmin"));
    
    // Combined policies (optional, for convenience)
    options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole("MasterAdmin", "Admin", "Teacher"));
    options.AddPolicy("AdminOrStudentOffice", policy => policy.RequireRole("MasterAdmin", "Admin", "StudentOffice"));
    options.AddPolicy("AdminOrFinance", policy => policy.RequireRole("MasterAdmin", "Admin", "Finance"));
});

// Configure SignalR
builder.Services.AddSignalR();

// Configure Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Get supported cultures from environment variable or use default
var supportedCulturesString = builder.Configuration["SupportedCultures"] ?? "en,az,tr,ru";
var cultureCodes = supportedCulturesString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var supportedCultures = cultureCodes.Select(c => new CultureInfo(c)).ToArray();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

var app = builder.Build();

// Migrate database on startup (wait for MySQL to be ready)
try
{
    await app.MigrateDatabaseAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database migration failed. Application will continue but may have issues.");
    // Don't throw - allow app to start even if migration fails
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS API v1");
        c.RoutePrefix = "swagger"; // Swagger UI available at /swagger
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.DefaultModelsExpandDepth(-1); // Hide models section by default
    });
}

// Global Exception Handling Middleware (must be early in pipeline)
app.UseMiddleware<LMS.API.Middleware.ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Serve static files from wwwroot
app.UseStaticFiles();

// Use Localization
app.UseRequestLocalization();

// Authentication & Authorization
app.UseAuthentication();
app.UseRoleAuthorization(); // Custom middleware for role-based authorization
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Map SignalR Hubs
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<QuizHub>("/quizHub");
app.MapHub<LMS.Infrastructure.Services.Notifications.NotificationHub>("/notificationsHub");

app.Run();
