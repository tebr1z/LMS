namespace LMS.Application.Interfaces.Payments;

/// <summary>
/// Interface for payment processing (supports Stripe, PayPal, etc.)
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a payment intent/session
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment session/intent details</returns>
    Task<PaymentSession> CreatePaymentSessionAsync(PaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms/processes a payment
    /// </summary>
    /// <param name="paymentId">Payment ID or session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment result</returns>
    Task<PaymentResult> ConfirmPaymentAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="amount">Refund amount (null for full refund)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund result</returns>
    Task<RefundResult> RefundPaymentAsync(string paymentId, decimal? amount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment status
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment status</returns>
    Task<PaymentStatus> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a webhook signature (for payment provider callbacks)
    /// </summary>
    /// <param name="payload">Webhook payload</param>
    /// <param name="signature">Webhook signature</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> ValidateWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payment request model
/// </summary>
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

/// <summary>
/// Payment session model
/// </summary>
public class PaymentSession
{
    public string SessionId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Payment result model
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Refund result model
/// </summary>
public class RefundResult
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Payment status model
/// </summary>
public class PaymentStatus
{
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // pending, succeeded, failed, refunded
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

