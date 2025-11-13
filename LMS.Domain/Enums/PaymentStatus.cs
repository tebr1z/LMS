namespace LMS.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,    // Payment pending
    Completed = 2,  // Payment completed
    Late = 3,       // Payment overdue
    Manual = 4,     // Manual payment (cash/post-terminal)
    Deferred = 5    // Payment deferred/fiancé status (custom status for finance)
}

