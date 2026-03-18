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
        var battleTickets = await _ticketRepo.GetUnReviewedBattleTicketPurchasesAsync();
        var refreshTickets = await _ticketRepo.GetUnReviewedRefreshTicketPurchasesAsync();

        return Ok(new
        {
            BattleTicketPurchases = battleTickets,
            RefreshTicketPurchases = refreshTickets
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
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmTicketPurchase(int purchaseLogId, [FromQuery] string type = "battle")
    {
        try
        {
            if (type == "refresh")
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
