namespace ArenaService.Controllers;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Extensions;
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
    private readonly ILogger<TicketController> _logger;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ITicketRepository _ticketRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly ISeasonRepository _seasonRepo;

    public TicketController(
        ILogger<TicketController> logger,
        IBackgroundJobClient jobClient,
        ITicketRepository ticketRepo,
        ISeasonCacheRepository seasonCacheRepo,
        ISeasonRepository seasonRepo
    )
    {
        _logger = logger;
        _ticketRepo = ticketRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _seasonRepo = seasonRepo;
        _jobClient = jobClient;
    }

    [HttpGet("battle")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "TicketStatus", typeof(TicketStatusResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
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

            return Ok(BattleTicketStatusResponse.CreateBattleTicketDefault(season));
        }

        return Ok(
            BattleTicketStatusResponse.FromBattleStatusModels(
                battleTicketStatusPerSeason,
                battleTicketStatusPerRound
            )
        );
    }

    [HttpGet("refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "TicketStatus", typeof(TicketStatusResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
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

            return Ok(RefreshTicketStatusResponse.CreateRefreshTicketDefault(season));
        }

        return Ok(RefreshTicketStatusResponse.FromRefreshStatusModel(refreshTicketStatusPerRound));
    }

    [HttpPost("battle/purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status201Created, "Purchase Log Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Status400BadRequest", typeof(string))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status423Locked, "Status423Locked", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
    public async Task<IActionResult> PurchaseBattleTicket([FromBody] PurchaseTicketRequest request)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        _logger.LogInformation(
            $"Purchase Battle Ticket - From: {avatarAddress}, {request.TicketCount} {request.PurchasePrice} {request.TxId}"
        );

        var cachedBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        if (
            cachedRound.EndBlock - ArenaServiceConfig.PURCHASE_TICKET_BLOCK_THRESHOLD
            <= cachedBlockIndex
        )
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                "ROUND_ENDING"
            );
        }

        var inProgressPurchases = await _ticketRepo.GetInProgressBattleTicketPurchases(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );
        if (inProgressPurchases.Count > 0)
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                "PURCHASE_IN_PROGRESS"
            );
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

        if (battleTicketStatusPerRound is not null)
        {
            if (
                battleTicketStatusPerRound!.PurchaseCount + request.TicketCount
                > season.BattleTicketPolicy.MaxPurchasableTicketsPerRound
            )
            {
                return BadRequest("MAX_ROUND_TICKETS_REACHED");
            }
        }
        else
        {
            if (request.TicketCount > season.BattleTicketPolicy.MaxPurchasableTicketsPerRound)
            {
                return BadRequest("MAX_ROUND_TICKETS_REACHED");
            }
        }
        if (battleTicketStatusPerSeason is not null)
        {
            if (
                battleTicketStatusPerSeason!.PurchaseCount + request.TicketCount
                > season.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
            )
            {
                return BadRequest("MAX_SEASON_TICKETS_REACHED");
            }
        }
        else
        {
            if (request.TicketCount > season.BattleTicketPolicy.MaxPurchasableTicketsPerSeason)
            {
                return BadRequest("MAX_SEASON_TICKETS_REACHED");
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
            return BadRequest("INVALID_PURCHASE_PRICE");
        }

        var purchaseLog = await _ticketRepo.AddBattleTicketPurchaseLog(
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
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Status400BadRequest", typeof(string))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status423Locked, "Status423Locked", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
    public async Task<IActionResult> PurchaseRefreshTicket([FromBody] PurchaseTicketRequest request)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        _logger.LogInformation(
            $"Purchase Refresh Ticket - From: {avatarAddress}, {request.TicketCount} {request.PurchasePrice} {request.TxId}"
        );

        var cachedBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        if (
            cachedRound.EndBlock - ArenaServiceConfig.PURCHASE_TICKET_BLOCK_THRESHOLD
            <= cachedBlockIndex
        )
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                "ROUND_ENDING"
            );
        }

        var inProgressPurchases = await _ticketRepo.GetInProgressRefreshTicketPurchases(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );
        if (inProgressPurchases.Count > 0)
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                "PURCHASE_IN_PROGRESS"
            );
        }

        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );
        var season = await _seasonRepo.GetSeasonAsync(
            cachedSeason.Id,
            q => q.Include(s => s.RefreshTicketPolicy)
        );

        if (refreshTicketStatusPerRound is not null)
        {
            if (
                refreshTicketStatusPerRound!.PurchaseCount + request.TicketCount
                > season.RefreshTicketPolicy.MaxPurchasableTicketsPerRound
            )
            {
                return BadRequest("MAX_ROUND_TICKETS_REACHED");
            }
        }
        else
        {
            if (request.TicketCount > season.RefreshTicketPolicy.MaxPurchasableTicketsPerRound)
            {
                return BadRequest("MAX_ROUND_TICKETS_REACHED");
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
            return BadRequest("INVALID_PURCHASE_PRICE");
        }

        var purchaseLog = await _ticketRepo.AddRefreshTicketPurchaseLog(
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
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Status403Forbidden", typeof(string))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
    public async Task<IActionResult> GetPurchaseBattleTicketLog(int logId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var purchaseLog = await _ticketRepo.GetBattleTicketPurchaseLogById(logId);

        if (purchaseLog is null)
        {
            return NotFound("LOG_NOT_FOUND");
        }

        if (purchaseLog.AvatarAddress != avatarAddress)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                "UNAUTHORIZED_ACCESS"
            );
        }

        return Ok(purchaseLog.ToResponse());
    }

    [HttpGet("refresh/purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(TicketPurchaseLogResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Status403Forbidden", typeof(string))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
    public async Task<IActionResult> GetPurchaseRefreshTicketLog(int logId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var purchaseLog = await _ticketRepo.GetRefreshTicketPurchaseLogById(logId);

        if (purchaseLog is null)
        {
            return NotFound("LOG_NOT_FOUND");
        }

        if (purchaseLog.AvatarAddress != avatarAddress)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                "UNAUTHORIZED_ACCESS"
            );
        }

        return Ok(purchaseLog.ToResponse());
    }
}
