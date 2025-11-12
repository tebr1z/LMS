# LMS Modular Architecture

This document describes the modular architecture prepared for future feature additions.

## Architecture Overview

The LMS follows Clean Architecture principles with a modular design that allows easy addition of new features without modifying existing code.

## Module Structure

### 1. File Storage Module

**Location:** `LMS.Application/Interfaces/Storage/` and `LMS.Infrastructure/Services/Storage/`

**Interface:** `IFileStorageService`

**Implementations:**
- `LocalFileStorageService` - Stores files on local file system (fully implemented)
- `S3FileStorageService` - AWS S3 storage (placeholder - requires AWSSDK.S3 package)

**Configuration:**
```json
{
  "FileStorage": {
    "Provider": "Local", // or "S3"
    "LocalPath": "wwwroot/uploads",
    "S3": {
      "BucketName": "your-bucket",
      "Region": "us-east-1"
    }
  }
}
```

**Usage:**
```csharp
public class MyService
{
    private readonly IFileStorageService _fileStorage;
    
    public async Task<string> UploadFile(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        return await _fileStorage.UploadFileAsync(
            stream, 
            file.FileName, 
            file.ContentType,
            folder: "courses");
    }
}
```

**To Add New Storage Provider:**
1. Create new class implementing `IFileStorageService`
2. Register in `DependencyInjection.cs` based on configuration

---

### 2. Notification Module

**Location:** `LMS.Application/Interfaces/Notifications/` and `LMS.Infrastructure/Services/Notifications/`

**Interfaces:**
- `IEmailService` - Email notifications
- `IRealTimeNotificationService` - Real-time notifications (SignalR)
- `INotificationService` - Unified notification service

**Implementations:**
- `EmailService` - SMTP email (placeholder - configure SMTP settings)
- `SignalRNotificationService` - Real-time notifications via SignalR (fully implemented)
- `NotificationService` - Combines email and real-time notifications

**Configuration:**
```json
{
  "Email": {
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email",
      "Password": "your-password",
      "From": "noreply@lms.com"
    }
  }
}
```

**Usage:**
```csharp
public class MyService
{
    private readonly INotificationService _notificationService;
    
    public async Task NotifyUser(int userId, string email)
    {
        var notification = new NotificationMessage
        {
            Title = "Welcome",
            Message = "Welcome to LMS!",
            Type = "success"
        };
        
        await _notificationService.SendNotificationAsync(
            userId, 
            email, 
            notification,
            sendEmail: true,
            sendRealTime: true);
    }
}
```

**To Add New Email Provider:**
1. Create new class implementing `IEmailService` (e.g., SendGrid, Mailgun)
2. Register in `DependencyInjection.cs`

---

### 3. Payment Module

**Location:** `LMS.Application/Interfaces/Payments/` and `LMS.Infrastructure/Services/Payments/`

**Interface:** `IPaymentService`

**Implementations:**
- `StripePaymentService` - Stripe integration (placeholder - requires Stripe.net package)
- `PayPalPaymentService` - PayPal integration (placeholder - requires PayPal SDK)

**Configuration:**
```json
{
  "Payments": {
    "Provider": "Stripe", // or "PayPal"
    "Stripe": {
      "SecretKey": "sk_test_...",
      "PublishableKey": "pk_test_..."
    },
    "PayPal": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "Mode": "sandbox" // or "live"
    }
  }
}
```

**Usage:**
```csharp
public class PaymentService
{
    private readonly IPaymentService _paymentService;
    
    public async Task<PaymentSession> CreatePayment(decimal amount, int userId)
    {
        var request = new PaymentRequest
        {
            Amount = amount,
            Currency = "USD",
            Description = "Course Enrollment",
            UserId = userId,
            UserEmail = "user@example.com"
        };
        
        return await _paymentService.CreatePaymentSessionAsync(request);
    }
}
```

**To Add New Payment Provider:**
1. Create new class implementing `IPaymentService`
2. Register in `DependencyInjection.cs` based on configuration

---

### 4. Admin Panel Module

**Location:** `LMS.Application/Interfaces/Admin/` and `LMS.API/Controllers/Admin/`

**Interface:** `IAdminService`

**Implementation:** `AdminService`

**Controller:** `AdminController`

**Endpoints:**
- `GET /api/admin/statistics` - System statistics
- `GET /api/admin/users` - User management (paginated)
- `PUT /api/admin/users/{userId}/role` - Update user role
- `GET /api/admin/courses` - Course management (paginated)
- `GET /api/admin/enrollments/statistics` - Enrollment statistics

**Authorization:** All endpoints require `Admin` role

**Usage:**
```csharp
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var stats = await _adminService.GetSystemStatisticsAsync();
        return Ok(stats);
    }
}
```

---

## Adding New Modules

### Step-by-Step Guide

1. **Define Interface in Application Layer**
   ```
   LMS.Application/Interfaces/YourModule/IYourService.cs
   ```

2. **Create Implementation in Infrastructure Layer**
   ```
   LMS.Infrastructure/Services/YourModule/YourService.cs
   ```

3. **Register in Dependency Injection**
   ```csharp
   // In LMS.Infrastructure/DependencyInjection.cs
   services.AddScoped<IYourService, YourService>();
   ```

4. **Create Controllers (if needed)**
   ```
   LMS.API/Controllers/YourModule/YourController.cs
   ```

5. **Add Configuration**
   ```json
   // In appsettings.json
   {
     "YourModule": {
       "Setting1": "value1",
       "Setting2": "value2"
     }
   }
   ```

---

## Configuration-Based Module Selection

The architecture supports configuration-based selection of implementations:

```csharp
// Example from DependencyInjection.cs
var storageProvider = configuration["FileStorage:Provider"] ?? "Local";
if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    services.AddScoped<IFileStorageService, S3FileStorageService>();
}
else
{
    services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
```

This allows switching between implementations without code changes.

---

## Benefits of Modular Architecture

1. **Separation of Concerns** - Each module is independent
2. **Easy Testing** - Interfaces allow mocking
3. **Flexibility** - Swap implementations via configuration
4. **Scalability** - Add new features without modifying existing code
5. **Maintainability** - Clear boundaries and responsibilities

---

## Future Module Ideas

- **Analytics Module** - Track user behavior, course performance
- **Reporting Module** - Generate reports (PDF, Excel)
- **Search Module** - Full-text search (Elasticsearch, Algolia)
- **Caching Module** - Redis caching layer
- **Audit Module** - Track all system changes
- **Export/Import Module** - Bulk data operations
- **Integration Module** - Third-party integrations (Zoom, Google Meet)

---

## Best Practices

1. **Always define interfaces in Application layer**
2. **Implement in Infrastructure layer**
3. **Use dependency injection for all services**
4. **Make implementations swappable via configuration**
5. **Follow single responsibility principle**
6. **Keep modules independent**
7. **Document module-specific configuration**

---

## Example: Adding a New Module

### 1. Create Interface
```csharp
// LMS.Application/Interfaces/Analytics/IAnalyticsService.cs
public interface IAnalyticsService
{
    Task<TrackingData> TrackEventAsync(string eventName, Dictionary<string, object> properties);
}
```

### 2. Create Implementation
```csharp
// LMS.Infrastructure/Services/Analytics/AnalyticsService.cs
public class AnalyticsService : IAnalyticsService
{
    public async Task<TrackingData> TrackEventAsync(string eventName, Dictionary<string, object> properties)
    {
        // Implementation
    }
}
```

### 3. Register Service
```csharp
// LMS.Infrastructure/DependencyInjection.cs
services.AddScoped<IAnalyticsService, AnalyticsService>();
```

### 4. Use in Controllers/Services
```csharp
public class CourseController : ControllerBase
{
    private readonly IAnalyticsService _analytics;
    
    [HttpPost]
    public async Task<ActionResult> CreateCourse(CreateCourseCommand command)
    {
        // Create course...
        await _analytics.TrackEventAsync("course_created", new Dictionary<string, object>
        {
            { "courseId", courseId },
            { "userId", userId }
        });
    }
}
```

---

## Current Module Status

| Module | Status | Implementation |
|--------|--------|----------------|
| File Storage | ✅ Ready | Local (complete), S3 (placeholder) |
| Email Notifications | ✅ Ready | SMTP (placeholder) |
| Real-time Notifications | ✅ Ready | SignalR (complete) |
| Payment Processing | ✅ Ready | Stripe/PayPal (placeholders) |
| Admin Panel | ✅ Ready | Complete |

---

## Notes

- Placeholder implementations throw `NotImplementedException` with instructions
- All interfaces are fully defined and ready for implementation
- Configuration structure is prepared for all modules
- Dependency injection is configured for easy swapping

