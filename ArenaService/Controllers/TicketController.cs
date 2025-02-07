namespace ArenaService.Controllers;

using ArenaService.Shared.Constants;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using ArenaService.Worker;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("tickets")]
[ApiController]
public class TicketController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly ITicketRepository _ticketRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly ISeasonRepository _seasonRepo;

    public TicketController(
        IBackgroundJobClient jobClient,
        ITicketRepository ticketRepo,
        ISeasonCacheRepository seasonCacheRepo,
        ISeasonRepository seasonRepo
    )
    {
        _ticketRepo = ticketRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _seasonRepo = seasonRepo;
        _jobClient = jobClient;
    }

    [HttpGet("battle")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "TicketStatus", typeof(TicketStatusResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<IActionResult> GetBattleTicketStatus()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var battleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            cachedSeason.Id,
            avatarAddress
        );
        var battleTicketStatusPerRound = await _ticketRepo.GetBattleTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );

        if (battleTicketStatusPerSeason is null || battleTicketStatusPerRound is null)
        {
            var season = await _seasonRepo.GetSeasonAsync(
                cachedSeason.Id,
                q => q.Include(s => s.BattleTicketPolicy)
            );

            return Ok(TicketStatusResponse.CreateBattleTicketDefault(season));
        }

        return Ok(
            TicketStatusResponse.FromBattleStatusModels(
                battleTicketStatusPerSeason,
                battleTicketStatusPerRound
            )
        );
    }

    [HttpGet("refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "TicketStatus", typeof(TicketStatusResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<IActionResult> GetRefreshTicketStatus()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );

        if (refreshTicketStatusPerRound is null)
        {
            var season = await _seasonRepo.GetSeasonAsync(
                cachedSeason.Id,
                q => q.Include(s => s.RefreshTicketPolicy)
            );

            return Ok(TicketStatusResponse.CreateRefreshTicketDefault(season));
        }

        return Ok(TicketStatusResponse.FromRefreshStatusModel(refreshTicketStatusPerRound));
    }

    [HttpPost("battle/purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status201Created, "Purchase Log Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status423Locked, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<IActionResult> PurchaseBattleTicket([FromBody] PurchaseTicketRequest request)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        if (
            cachedRound.EndBlock - ArenaServiceConfig.PURCHASE_TICKET_BLOCK_THRESHOLD
            <= cachedBlockIndex
        )
        {
            return StatusCode(StatusCodes.Status423Locked);
        }

        var inProgressPurchases = await _ticketRepo.GetInProgressBattleTicketPurchases(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );
        if (inProgressPurchases.Count > 0)
        {
            return StatusCode(StatusCodes.Status423Locked);
        }

        var battleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            cachedSeason.Id,
            avatarAddress
        );
        var battleTicketStatusPerRound = await _ticketRepo.GetBattleTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );
        var season = await _seasonRepo.GetSeasonAsync(
            cachedSeason.Id,
            q => q.Include(s => s.BattleTicketPolicy)
        );

        BattleTicketPurchaseLog purchaseLog;
        if (battleTicketStatusPerRound is not null)
        {
            if (
                battleTicketStatusPerRound!.PurchaseCount + request.TicketCount
                > season.BattleTicketPolicy.MaxPurchasableTicketsPerRound
            )
            {
                return BadRequest("Max purchaseable ticket reached");
            }
        }

        var requiredAmount = 0m;

        var purchasedCount = battleTicketStatusPerSeason is null
            ? 0
            : battleTicketStatusPerSeason.PurchaseCount;

        for (int i = 0; i < request.TicketCount; i++)
        {
            requiredAmount += season.BattleTicketPolicy.GetPrice(purchasedCount + i);
        }

        if (request.PurchasePrice != requiredAmount)
        {
            return BadRequest($"{request.PurchasePrice} {requiredAmount}");
        }

        purchaseLog = await _ticketRepo.AddBattleTicketPurchaseLog(
            cachedSeason.Id,
            cachedRound.Id,
            avatarAddress,
            request.TxId,
            request.TicketCount
        );

        _jobClient.Enqueue<PurchaseBattleTicketProcessor>(processor =>
            processor.ProcessAsync(purchaseLog.Id)
        );

        var locationUri = Url.Action(
            nameof(GetPurchaseBattleTicketLog),
            new { purchaseLogId = purchaseLog.Id }
        );

        return Created(locationUri, purchaseLog.Id);
    }

    [HttpPost("refresh/purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status201Created, "Purchase Log Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status423Locked, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<IActionResult> PurchaseRefreshTicket([FromBody] PurchaseTicketRequest request)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        if (
            cachedRound.EndBlock - ArenaServiceConfig.PURCHASE_TICKET_BLOCK_THRESHOLD
            <= cachedBlockIndex
        )
        {
            return StatusCode(StatusCodes.Status423Locked);
        }

        var inProgressPurchases = await _ticketRepo.GetInProgressRefreshTicketPurchases(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );
        if (inProgressPurchases.Count > 0)
        {
            return StatusCode(StatusCodes.Status423Locked);
        }

        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );
        var season = await _seasonRepo.GetSeasonAsync(
            cachedSeason.Id,
            q => q.Include(s => s.RefreshTicketPolicy)
        );

        RefreshTicketPurchaseLog purchaseLog;
        if (refreshTicketStatusPerRound is not null)
        {
            if (
                refreshTicketStatusPerRound!.PurchaseCount + request.TicketCount
                > season.RefreshTicketPolicy.MaxPurchasableTicketsPerRound
            )
            {
                return BadRequest("Max purchaseable ticket reached");
            }
        }

        var requiredAmount = 0m;

        var purchasedCount = refreshTicketStatusPerRound is null
            ? 0
            : refreshTicketStatusPerRound.PurchaseCount;

        for (int i = 0; i < request.TicketCount; i++)
        {
            requiredAmount += season.RefreshTicketPolicy.GetPrice(purchasedCount + i);
        }

        if (request.PurchasePrice != requiredAmount)
        {
            return BadRequest($"{request.PurchasePrice} {requiredAmount}");
        }

        purchaseLog = await _ticketRepo.AddRefreshTicketPurchaseLog(
            cachedSeason.Id,
            cachedRound.Id,
            avatarAddress,
            request.TxId,
            request.TicketCount
        );

        _jobClient.Enqueue<PurchaseRefreshTicketProcessor>(processor =>
            processor.ProcessAsync(purchaseLog.Id)
        );

        var locationUri = Url.Action(
            nameof(GetPurchaseRefreshTicketLog),
            new { purchaseLogId = purchaseLog.Id }
        );

        return Created(locationUri, purchaseLog.Id);
    }

    [HttpGet("battle/purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(TicketPurchaseLogResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<IActionResult> GetPurchaseBattleTicketLog(int logId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var purchaseLog = await _ticketRepo.GetBattleTicketPurchaseLogById(logId);

        if (purchaseLog is null)
        {
            return NotFound($"Not found purchase log {logId}");
        }

        if (purchaseLog.AvatarAddress != avatarAddress)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(purchaseLog.ToResponse());
    }

    [HttpGet("refresh/purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(TicketPurchaseLogResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<IActionResult> GetPurchaseRefreshTicketLog(int logId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var purchaseLog = await _ticketRepo.GetRefreshTicketPurchaseLogById(logId);

        if (purchaseLog is null)
        {
            return NotFound($"Not found purchase log {logId}");
        }

        if (purchaseLog.AvatarAddress != avatarAddress)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(purchaseLog.ToResponse());
    }
}
