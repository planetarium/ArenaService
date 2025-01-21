namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Models.BattleTicket;
using ArenaService.Models.Enums;
using ArenaService.Models.RefreshTicket;
using ArenaService.Repositories;
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
    public async Task<
        Results<NotFound<string>, StatusCodeHttpResult, Ok<TicketStatusResponse>>
    > GetBattleTicketStatus()
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

            return TypedResults.Ok(TicketStatusResponse.CreateBattleTicketDefault(season));
        }

        return TypedResults.Ok(
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
    public async Task<
        Results<NotFound<string>, StatusCodeHttpResult, Ok<TicketStatusResponse>>
    > GetRefreshTicketStatus()
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

            return TypedResults.Ok(TicketStatusResponse.CreateRefreshTicketDefault(season));
        }

        return TypedResults.Ok(
            TicketStatusResponse.FromRefreshStatusModel(refreshTicketStatusPerRound)
        );
    }

    [HttpPost("battle/purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<Results<NotFound<string>, BadRequest<string>, Ok<int>>> PurchaseBattleTicket(
        [FromBody] PurchaseTicketRequest request
    )
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
                return TypedResults.BadRequest("Max purchaseable ticket reached");
            }
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

        return TypedResults.Ok(purchaseLog.Id);
    }

    [HttpPost("refresh/purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<Results<NotFound<string>, BadRequest<string>, Ok<int>>> PurchaseRefreshTicket(
        [FromBody] PurchaseTicketRequest request
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

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
                return TypedResults.BadRequest("Max purchaseable ticket reached");
            }
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

        return TypedResults.Ok(purchaseLog.Id);
    }

    [HttpGet("battle/purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(TicketPurchaseLogResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<NotFound<string>, StatusCodeHttpResult, Ok<TicketPurchaseLogResponse>>
    > GetPurchaseBattleTicketLog(int logId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var purchaseLog = await _ticketRepo.GetBattleTicketPurchaseLogById(logId);

        if (purchaseLog is null)
        {
            return TypedResults.NotFound($"Not found purchase log {logId}");
        }

        if (purchaseLog.AvatarAddress != avatarAddress)
        {
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(purchaseLog.ToResponse());
    }

    [HttpGet("refresh/purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(TicketPurchaseLogResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<NotFound<string>, StatusCodeHttpResult, Ok<TicketPurchaseLogResponse>>
    > GetPurchaseRefreshTicketLog(int logId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var purchaseLog = await _ticketRepo.GetRefreshTicketPurchaseLogById(logId);

        if (purchaseLog is null)
        {
            return TypedResults.NotFound($"Not found purchase log {logId}");
        }

        if (purchaseLog.AvatarAddress != avatarAddress)
        {
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(purchaseLog.ToResponse());
    }
}
