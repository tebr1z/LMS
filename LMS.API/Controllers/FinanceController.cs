using LMS.Application.Features.Finance.Commands.AcceptManualPayment;
using LMS.Application.Features.Finance.Commands.CreateMonthlyInvoice;
using LMS.Application.Features.Finance.Commands.DeferPayment;
using LMS.Application.Features.Finance.Commands.MarkOverduePayments;
using LMS.Application.Features.Finance.Commands.ProcessWebhookPayment;
using LMS.Application.Features.Finance.Queries.GetPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FinanceController> _logger;

    public FinanceController(IMediator mediator, ILogger<FinanceController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all payments (Finance and Admin only)
    /// </summary>
    [HttpGet("payments")]
    [Authorize(Roles = "Finance,Admin,MasterAdmin")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments(
        [FromQuery] int? studentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Domain.Enums.PaymentStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.PaymentStatus>(status, true, out var parsedStatus))
            {
                statusEnum = parsedStatus;
            }

            Domain.Enums.PaymentMethod? methodEnum = null;
            if (!string.IsNullOrEmpty(paymentMethod) && Enum.TryParse<Domain.Enums.PaymentMethod>(paymentMethod, true, out var parsedMethod))
            {
                methodEnum = parsedMethod;
            }

            var query = new GetPaymentsQuery
            {
                StudentId = studentId,
                Status = statusEnum,
                PaymentMethod = methodEnum
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return StatusCode(500, new { message = "An error occurred while retrieving payments." });
        }
    }

    /// <summary>
    /// Accept manual payment (cash/post-terminal) - Finance and Admin only
    /// </summary>
    [HttpPost("payments/manual")]
    [Authorize(Roles = "Finance,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> AcceptManualPayment(
        [FromBody] AcceptManualPaymentCommandDto commandDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new AcceptManualPaymentCommand
            {
                PaymentId = commandDto.PaymentId,
                Reference = commandDto.Reference,
                Notes = commandDto.Notes,
                AcceptedById = userId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { PaymentId = result, Message = "Manual payment accepted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when accepting manual payment");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when accepting manual payment");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting manual payment");
            return StatusCode(500, new { message = "An error occurred while accepting manual payment." });
        }
    }

    /// <summary>
    /// Defer payment (fiancé status) - Finance and Admin only
    /// </summary>
    [HttpPost("payments/{paymentId}/defer")]
    [Authorize(Roles = "Finance,Admin,MasterAdmin")]
    public async Task<ActionResult> DeferPayment(
        int paymentId,
        [FromBody] DeferPaymentCommandDto? commandDto = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new DeferPaymentCommand
            {
                PaymentId = paymentId,
                Notes = commandDto?.Notes,
                DeferredById = userId
            };

            await _mediator.Send(command, cancellationToken);
            return Ok(new { Message = "Payment deferred successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deferring payment");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when deferring payment");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deferring payment");
            return StatusCode(500, new { message = "An error occurred while deferring payment." });
        }
    }

    /// <summary>
    /// Webhook endpoint for payment provider (OneBank, etc.)
    /// No authentication required (uses webhook secret/signature validation in production)
    /// </summary>
    [HttpPost("webhook/payment")]
    [AllowAnonymous] // In production, validate webhook signature instead
    public async Task<ActionResult> ProcessWebhookPayment(
        [FromBody] WebhookPaymentDto webhookDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In production, validate webhook signature here
            // var isValid = ValidateWebhookSignature(Request.Headers, Request.Body);
            // if (!isValid) return Unauthorized();

            var command = new ProcessWebhookPaymentCommand
            {
                ProviderTransactionId = webhookDto.TransactionId ?? string.Empty,
                Reference = webhookDto.Reference,
                Amount = webhookDto.Amount,
                Currency = webhookDto.Currency ?? "AZN",
                PaidAt = webhookDto.PaidAt ?? DateTime.UtcNow,
                ProviderData = webhookDto.AdditionalData
            };

            await _mediator.Send(command, cancellationToken);
            return Ok(new { Message = "Webhook processed successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when processing webhook payment");
            // Return 200 to prevent provider retries for invalid data
            return Ok(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook payment");
            // Return 200 to prevent provider retries
            return Ok(new { Message = "Webhook received but could not be processed." });
        }
    }

    /// <summary>
    /// Mark overdue payments as Late (can be called by scheduler or manually)
    /// Finance and Admin only
    /// </summary>
    [HttpPost("payments/mark-overdue")]
    [Authorize(Roles = "Finance,Admin,MasterAdmin")]
    public async Task<ActionResult> MarkOverduePayments(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new MarkOverduePaymentsCommand();
            var count = await _mediator.Send(command, cancellationToken);
            return Ok(new { Message = $"Marked {count} payments as Late." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking overdue payments");
            return StatusCode(500, new { message = "An error occurred while marking overdue payments." });
        }
    }

    /// <summary>
    /// Create monthly invoice - Finance and Admin only
    /// </summary>
    [HttpPost("invoices/monthly")]
    [Authorize(Roles = "Finance,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> CreateMonthlyInvoice(
        [FromBody] CreateMonthlyInvoiceCommandDto commandDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new CreateMonthlyInvoiceCommand
            {
                StudentId = commandDto.StudentId,
                Amount = commandDto.Amount,
                Currency = commandDto.Currency ?? "AZN",
                DueDate = commandDto.DueDate,
                Description = commandDto.Description,
                CreatedById = userId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetPayments), new { }, new { InvoiceId = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating monthly invoice");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when creating monthly invoice");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating monthly invoice");
            return StatusCode(500, new { message = "An error occurred while creating monthly invoice." });
        }
    }
}

public class AcceptManualPaymentCommandDto
{
    public int PaymentId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class DeferPaymentCommandDto
{
    public string? Notes { get; set; }
}

public class WebhookPaymentDto
{
    public string? TransactionId { get; set; }
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? PaidAt { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class CreateMonthlyInvoiceCommandDto
{
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime DueDate { get; set; }
    public string? Description { get; set; }
}

