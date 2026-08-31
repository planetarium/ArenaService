using ArenaService.BackOffice.Authentication;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Models.Ticket;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaService.BackOffice.Controllers;

/// <summary>
/// Review queue for battles and ticket purchases whose transactions need a manual check.
/// Mirrors the ConfirmTx Blazor page.
/// </summary>
[ApiController]
[Route("api/confirm-tx")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class ConfirmTxController : ControllerBase
{
    private const string BattleTicketType = "battle";
    private const string RefreshTicketType = "refresh";

    private static readonly HashSet<string> ValidTicketTypes =
        new(new[] { BattleTicketType, RefreshTicketType }, StringComparer.OrdinalIgnoreCase);

    private readonly IBattleRepository _battleRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<ConfirmTxController> _logger;

    public ConfirmTxController(
        IBattleRepository battleRepository,
        ITicketRepository ticketRepository,
        ILogger<ConfirmTxController> logger
    )
    {
        _battleRepository = battleRepository;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    /// <summary>Lists battles awaiting review.</summary>
    [HttpGet("battles")]
    public async Task<ActionResult<ApiResponse<List<BattleDto>>>> GetUnreviewedBattles()
    {
        var battles = await _battleRepository.GetUnReviewedBattlesAsync();
        return Ok(ApiResponse<List<BattleDto>>.Ok(battles.Select(BattleDto.From).ToList()));
    }

    /// <summary>Lists battle and refresh ticket purchases awaiting review.</summary>
    [HttpGet("ticket-purchases")]
    public async Task<ActionResult<ApiResponse<List<TicketPurchaseLogDto>>>> GetUnreviewedTicketPurchases()
    {
        var battlePurchasesTask = _ticketRepository.GetUnReviewedBattleTicketPurchasesAsync();
        var refreshPurchasesTask = _ticketRepository.GetUnReviewedRefreshTicketPurchasesAsync();
        await Task.WhenAll(battlePurchasesTask, refreshPurchasesTask);

        var purchases = battlePurchasesTask.Result
            .Cast<TicketPurchaseLog>()
            .Concat(refreshPurchasesTask.Result)
            .ToList();

        return Ok(
            ApiResponse<List<TicketPurchaseLogDto>>.Ok(
                purchases.Select(TicketPurchaseLogDto.From).ToList()
            )
        );
    }

    /// <summary>Marks a battle as reviewed.</summary>
    [HttpPost("battles/{battleId:int}/review")]
    public async Task<ActionResult<ApiResponse>> MarkBattleAsReviewed(int battleId)
    {
        try
        {
            await _battleRepository.UpdateBattle(battleId, battle => battle.Reviewed = true);
            return Ok(ApiResponse.Ok($"Battle {battleId} marked as reviewed."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark battle {BattleId} as reviewed.", battleId);
            return StatusCode(500, ApiResponse.Error($"Battle review failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Marks a ticket purchase log as reviewed.
    /// </summary>
    /// <param name="purchaseLogId">Id of the purchase log.</param>
    /// <param name="type">
    /// Which log the id belongs to: <c>battle</c> or <c>refresh</c>. Case insensitive.
    /// The list endpoint returns this as <c>ticketType</c> on every entry.
    /// </param>
    [HttpPost("ticket-purchases/{purchaseLogId:int}/review")]
    public async Task<ActionResult<ApiResponse>> MarkTicketPurchaseAsReviewed(
        int purchaseLogId,
        [FromQuery] string type = BattleTicketType
    )
    {
        if (!ValidTicketTypes.Contains(type))
        {
            return BadRequest(
                ApiResponse.Error(
                    $"Unsupported ticket type '{type}'. Expected '{BattleTicketType}' or '{RefreshTicketType}'."
                )
            );
        }

        var isRefresh = string.Equals(type, RefreshTicketType, StringComparison.OrdinalIgnoreCase);

        try
        {
            if (isRefresh)
            {
                await _ticketRepository.UpdateRefreshTicketPurchaseLog(
                    purchaseLogId,
                    log => log.Reviewed = true
                );
            }
            else
            {
                await _ticketRepository.UpdateBattleTicketPurchaseLog(
                    purchaseLogId,
                    log => log.Reviewed = true
                );
            }

            return Ok(ApiResponse.Ok($"Ticket purchase {purchaseLogId} marked as reviewed."));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to mark {Type} ticket purchase {PurchaseLogId} as reviewed.",
                type,
                purchaseLogId
            );
            return StatusCode(500, ApiResponse.Error($"Ticket purchase review failed: {ex.Message}"));
        }
    }
}
