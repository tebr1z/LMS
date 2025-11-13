using LMS.Application.Features.StudentPayments.Commands.InitiatePayment;
using LMS.Application.Features.StudentPayments.Queries.GetStudentPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/student/payments")]
[Authorize]
public class StudentPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StudentPaymentsController> _logger;

    public StudentPaymentsController(IMediator mediator, ILogger<StudentPaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get student's payments and invoices (Student can only view their own)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<StudentPaymentsDto>> GetStudentPayments(
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

            var query = new GetStudentPaymentsQuery
            {
                StudentId = userId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student payments");
            return StatusCode(500, new { message = "An error occurred while retrieving payments." });
        }
    }

    /// <summary>
    /// Initiate payment - returns redirect URL or payment session to bank
    /// </summary>
    [HttpPost("pay")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<PaymentSessionDto>> InitiatePayment(
        [FromBody] InitiatePaymentCommandDto commandDto,
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

            var command = new InitiatePaymentCommand
            {
                PaymentId = commandDto.PaymentId,
                StudentId = userId // Ensure student can only pay their own payments
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when initiating payment");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when initiating payment");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return StatusCode(500, new { message = "An error occurred while initiating payment." });
        }
    }
}

public class InitiatePaymentCommandDto
{
    public int PaymentId { get; set; }
}

