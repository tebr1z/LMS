using LMS.Application.Features.Messages.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MessagesController> _logger;
    private readonly IStringLocalizer<MessagesController> _localizer;

    public MessagesController(IMediator mediator, ILogger<MessagesController> logger, IStringLocalizer<MessagesController> localizer)
    {
        _mediator = mediator;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpPost]
    [Authorize(Roles = "Student,Teacher,Admin,MasterAdmin")] // Mentor excluded - read-only
    public async Task<ActionResult<int>> SendMessage([FromBody] SendMessageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { messageId = result, message = _localizer["MessageSentSuccess"] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["ErrorSendingMessage"]);
            return BadRequest(new { message = ex.Message });
        }
    }
}

