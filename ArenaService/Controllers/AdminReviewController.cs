namespace ArenaService.Controllers;

using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("admin/reviews")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReviewController : ControllerBase
{
    private readonly IBattleRepository _battleRepo;
    private readonly ITicketRepository _ticketRepo;

    private static readonly HashSet<string> ValidTicketTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "battle",
        "refresh"
    };

    public AdminReviewController(
        IBattleRepository battleRepo,
        ITicketRepository ticketRepo
    )
    {
        _battleRepo = battleRepo;
        _ticketRepo = ticketRepo;
    }

    [HttpGet("battles")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreviewedBattles()
    {
        var battles = await _battleRepo.GetUnReviewedBattlesAsync();
        return Ok(battles);
    }

    [HttpGet("ticket-purchases")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreviewedTicketPurchases()
    {
        var battleTicketsTask = _ticketRepo.GetUnReviewedBattleTicketPurchasesAsync();
        var refreshTicketsTask = _ticketRepo.GetUnReviewedRefreshTicketPurchasesAsync();

        await Task.WhenAll(battleTicketsTask, refreshTicketsTask);

        return Ok(new
        {
            BattleTicketPurchases = battleTicketsTask.Result,
            RefreshTicketPurchases = refreshTicketsTask.Result
        });
    }

    [HttpPost("battles/{battleId}/confirm")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmBattle(int battleId)
    {
        try
        {
            var battle = await _battleRepo.UpdateBattle(battleId, battle => battle.Reviewed = true);
            return Ok(battle);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpPost("ticket-purchases/{purchaseLogId}/confirm")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmTicketPurchase(int purchaseLogId, [FromQuery] string type = "battle")
    {
        if (!ValidTicketTypes.Contains(type))
        {
            return BadRequest($"Invalid ticket type '{type}'. Must be 'battle' or 'refresh'.");
        }

        try
        {
            if (string.Equals(type, "refresh", StringComparison.OrdinalIgnoreCase))
            {
                var log = await _ticketRepo.UpdateRefreshTicketPurchaseLog(purchaseLogId, log => log.Reviewed = true);
                return Ok(log);
            }
            else
            {
                var log = await _ticketRepo.UpdateBattleTicketPurchaseLog(purchaseLogId, log => log.Reviewed = true);
                return Ok(log);
            }
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }
}
