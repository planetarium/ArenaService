namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
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
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<AdminBattleReviewResponse>))]
    public async Task<IActionResult> GetUnreviewedBattles()
    {
        var battles = await _battleRepo.GetUnReviewedBattlesAsync();
        var response = battles.Select(b => new AdminBattleReviewResponse
        {
            Id = b.Id,
            AvatarAddress = b.AvatarAddress.ToString().ToLower(),
            SeasonId = b.SeasonId,
            RoundId = b.RoundId,
            BattleStatus = b.BattleStatus,
            TxId = b.TxId,
            TxStatus = b.TxStatus,
            ExceptionNames = b.ExceptionNames,
            Reviewed = b.Reviewed
        }).ToList();
        return Ok(response);
    }

    [HttpGet("ticket-purchases")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreviewedTicketPurchases()
    {
        var battleTicketsTask = _ticketRepo.GetUnReviewedBattleTicketPurchasesAsync();
        var refreshTicketsTask = _ticketRepo.GetUnReviewedRefreshTicketPurchasesAsync();

        await Task.WhenAll(battleTicketsTask, refreshTicketsTask);

        var battleResponse = battleTicketsTask.Result.Select(t => new AdminTicketPurchaseReviewResponse
        {
            Id = t.Id,
            AvatarAddress = t.AvatarAddress.ToString().ToLower(),
            SeasonId = t.SeasonId,
            RoundId = t.RoundId,
            PurchaseStatus = t.PurchaseStatus,
            TxId = t.TxId,
            TxStatus = t.TxStatus,
            ExceptionNames = t.ExceptionNames,
            Reviewed = t.Reviewed
        }).ToList();

        var refreshResponse = refreshTicketsTask.Result.Select(t => new AdminTicketPurchaseReviewResponse
        {
            Id = t.Id,
            AvatarAddress = t.AvatarAddress.ToString().ToLower(),
            SeasonId = t.SeasonId,
            RoundId = t.RoundId,
            PurchaseStatus = t.PurchaseStatus,
            TxId = t.TxId,
            TxStatus = t.TxStatus,
            ExceptionNames = t.ExceptionNames,
            Reviewed = t.Reviewed
        }).ToList();

        return Ok(new
        {
            BattleTicketPurchases = battleResponse,
            RefreshTicketPurchases = refreshResponse
        });
    }

    [HttpPost("battles/{battleId}/confirm")]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AdminBattleReviewResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmBattle(int battleId)
    {
        try
        {
            var battle = await _battleRepo.UpdateBattle(battleId, battle => battle.Reviewed = true);
            return Ok(new AdminBattleReviewResponse
            {
                Id = battle.Id,
                AvatarAddress = battle.AvatarAddress.ToString().ToLower(),
                SeasonId = battle.SeasonId,
                RoundId = battle.RoundId,
                BattleStatus = battle.BattleStatus,
                TxId = battle.TxId,
                TxStatus = battle.TxStatus,
                ExceptionNames = battle.ExceptionNames,
                Reviewed = battle.Reviewed
            });
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpPost("ticket-purchases/{purchaseLogId}/confirm")]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AdminTicketPurchaseReviewResponse))]
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
                return Ok(new AdminTicketPurchaseReviewResponse
                {
                    Id = log.Id,
                    AvatarAddress = log.AvatarAddress.ToString().ToLower(),
                    SeasonId = log.SeasonId,
                    RoundId = log.RoundId,
                    PurchaseStatus = log.PurchaseStatus,
                    TxId = log.TxId,
                    TxStatus = log.TxStatus,
                    ExceptionNames = log.ExceptionNames,
                    Reviewed = log.Reviewed
                });
            }
            else
            {
                var log = await _ticketRepo.UpdateBattleTicketPurchaseLog(purchaseLogId, log => log.Reviewed = true);
                return Ok(new AdminTicketPurchaseReviewResponse
                {
                    Id = log.Id,
                    AvatarAddress = log.AvatarAddress.ToString().ToLower(),
                    SeasonId = log.SeasonId,
                    RoundId = log.RoundId,
                    PurchaseStatus = log.PurchaseStatus,
                    TxId = log.TxId,
                    TxStatus = log.TxStatus,
                    ExceptionNames = log.ExceptionNames,
                    Reviewed = log.Reviewed
                });
            }
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }
}
