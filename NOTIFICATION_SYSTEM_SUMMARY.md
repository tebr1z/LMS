# Automated Notification System - Implementation Summary

## ✅ **Overview**
The automated notification system is fully implemented and ready for use. It includes background workers for automated notifications, a notification entity for persistence, and API endpoints for managing notifications.

---

## 📋 **1. Background Workers (IHostedService)**

### ✅ **OverdueInvoiceNotificationService**
- **Schedule**: Runs daily (every 24 hours)
- **Purpose**: Checks for overdue invoices and sends notifications
- **Notifications**:
  - **To Students**: Informs about overdue invoices with details
  - **To Finance Users**: Alerts about students with overdue invoices
- **Channels**: Both Email and InApp
- **Type**: `invoice_overdue`

**Location**: `LMS.Infrastructure/Services/Background/OverdueInvoiceNotificationService.cs`

### ✅ **AssignmentDeadlineNotificationService**
- **Schedule**: Runs hourly (every 1 hour)
- **Purpose**: Checks assignments approaching deadlines
- **Notifications**:
  - **24 hours before deadline**: Notifies students and teachers
  - **3 hours before deadline**: Notifies students and teachers
  - Only notifies students who haven't submitted yet
- **Channels**: Both Email and InApp
- **Type**: `assignment_deadline`

**Location**: `LMS.Infrastructure/Services/Background/AssignmentDeadlineNotificationService.cs`

### ✅ **StudentThresholdNotificationService**
- **Schedule**: Runs every 6 hours
- **Purpose**: Monitors students falling below thresholds
- **Thresholds Checked**:
  - **Attendance**: < 75%
  - **Average Score**: < 60%
  - **Missing Assignments**: ≥ 3 past due
- **Actions**:
  - Creates `StudentFlag` record
  - Notifies StudentOffice users
- **Channels**: Both Email and InApp
- **Type**: `student_flag`

**Location**: `LMS.Infrastructure/Services/Background/StudentThresholdNotificationService.cs`

---

## 📦 **2. Notification Entity**

### ✅ **Properties**
```csharp
public class Notification : BaseEntity
{
    public int Id { get; set; }              // Auto-generated
    public int UserId { get; set; }          // Target user
    public string Title { get; set; }        // Notification title
    public string Body { get; set; }         // Notification message
    public string? Data { get; set; }        // JSON data (additional context)
    public bool IsRead { get; set; }         // Read status
    public DateTime CreatedAt { get; set; }  // Creation timestamp
    public DateTime? ReadAt { get; set; }    // Read timestamp
    public NotificationChannel Channel { get; set; } // InApp, Email, SMS, Both
    public string? Type { get; set; }        // e.g., "invoice_overdue", "assignment_deadline", "student_flag"
}
```

**Location**: `LMS.Domain/Entities/Notification.cs`

### ✅ **NotificationChannel Enum**
```csharp
public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    SMS = 3,
    Both = 4 // Both Email and InApp
}
```

**Location**: `LMS.Domain/Enums/NotificationChannel.cs`

---

## 🔌 **3. API Endpoints**

### ✅ **GET /api/notifications**
- **Description**: Get notifications for the current user
- **Query Parameters**:
  - `isRead` (optional): Filter by read status (true/false)
  - `count` (optional): Limit number of results
- **Response**: Array of `NotificationDto`
- **Authorization**: Required (authenticated users)

### ✅ **GET /api/notifications/unread-count**
- **Description**: Get count of unread notifications
- **Response**: `{ "Count": number }`
- **Authorization**: Required (authenticated users)

### ✅ **POST /api/notifications/{notificationId}/mark-read**
- **Description**: Mark a specific notification as read
- **Route Parameters**: `notificationId` (int)
- **Response**: `{ "Message": "Notification marked as read." }`
- **Authorization**: Required (authenticated users, can only mark own notifications)

### ✅ **POST /api/notifications/mark-all-read**
- **Description**: Mark all notifications as read for the current user
- **Response**: `{ "Message": "All notifications marked as read." }`
- **Authorization**: Required (authenticated users)

**Location**: `LMS.API/Controllers/NotificationsController.cs`

---

## 🏗️ **4. Infrastructure Components**

### ✅ **Repository**
- **Interface**: `INotificationRepository`
- **Implementation**: `NotificationRepository`
- **Methods**:
  - `GetByUserIdAsync(userId, isRead, count)`
  - `GetUnreadCountByUserIdAsync(userId)`
  - `MarkAsReadAsync(notificationId, userId)`
  - `MarkAllAsReadAsync(userId)`
  - `AddAsync(notification)`

**Locations**:
- `LMS.Application/Interfaces/INotificationRepository.cs`
- `LMS.Infrastructure/Repositories/NotificationRepository.cs`

### ✅ **CQRS Handlers**
- **GetNotificationsQuery/Handler**: Retrieves notifications for a user
- **MarkNotificationAsReadCommand/Handler**: Marks a notification as read
- **MarkAllNotificationsAsReadCommand/Handler**: Marks all notifications as read

**Locations**:
- `LMS.Application/Features/Notifications/`

### ✅ **Database Configuration**
- **Configuration**: `NotificationConfiguration`
- **Indexes**:
  - `IX_Notifications_User_IsRead` (UserId, IsRead)
  - `IX_Notifications_CreatedAt` (CreatedAt)
  - `IX_Notifications_Type` (Type)

**Location**: `LMS.Infrastructure/Data/Configurations/NotificationConfiguration.cs`

---

## 🚀 **5. Registration & Startup**

All background services are registered in `Program.cs`:

```csharp
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.OverdueInvoiceNotificationService>();
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.AssignmentDeadlineNotificationService>();
builder.Services.AddHostedService<LMS.Infrastructure.Services.Background.StudentThresholdNotificationService>();
```

**Location**: `LMS.API/Program.cs`

---

## 📊 **6. Notification Flow**

### **For Invoices:**
1. Daily job checks for overdue invoices
2. Groups invoices by student
3. Creates `Notification` records for students
4. Creates `Notification` records for Finance users
5. Sends Email + Real-time notifications via `INotificationService`
6. Persists to database

### **For Assignment Deadlines:**
1. Hourly job checks assignments with deadlines
2. Filters assignments due in 24h or 3h
3. Identifies students via enrollments or group members
4. Skips students who already submitted
5. Creates `Notification` records for students and teachers
6. Sends Email + Real-time notifications
7. Persists to database

### **For Student Thresholds:**
1. Every 6 hours, checks all students
2. Evaluates attendance, average score, and missing assignments
3. Creates `StudentFlag` if thresholds breached
4. Creates `Notification` records for StudentOffice users
5. Sends Email + Real-time notifications
6. Persists to database

---

## 🔧 **7. Configuration & Customization**

### **Thresholds** (in `StudentThresholdNotificationService`):
- `AttendanceThreshold`: 75% (minimum)
- `AverageScoreThreshold`: 60% (minimum)
- `MissingAssignmentsThreshold`: 3 (maximum)

**Note**: These can be moved to `SystemSettings` for dynamic configuration.

### **Timing**:
- Invoice checks: Daily at startup + 5 minutes
- Assignment checks: Hourly at startup + 2 minutes
- Threshold checks: Every 6 hours at startup + 10 minutes

---

## ✅ **8. Status: COMPLETE**

All requested features have been implemented:
- ✅ Daily job for overdue invoices
- ✅ Hourly job for assignment deadlines (24h/3h)
- ✅ Student threshold monitoring with StudentFlag creation
- ✅ Notification entity with all required properties
- ✅ API endpoints to read and mark notifications as read
- ✅ Email and InApp notification channels
- ✅ Database persistence
- ✅ Real-time notifications via SignalR

---

## 📝 **Next Steps (Optional Enhancements)**

1. **Move thresholds to SystemSettings** for dynamic configuration
2. **Add SMS channel support** (currently enum exists but not implemented)
3. **Add notification preferences** per user (email/SMS/InApp)
4. **Add notification templates** for consistent formatting
5. **Add notification history/archiving** for old notifications
6. **Add batch notification deletion** endpoint

---

**Last Updated**: 2024-11-13
**Status**: ✅ Production Ready

