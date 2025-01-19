namespace ArenaService.Controllers;

using ArenaService.Constants;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Views;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("available-opponents")]
[ApiController]
public class AvailableOpponentController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IRefreshRequestRepository _refreshRequestRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IParticipateService _participateService;
    private readonly IRefreshPriceRepository _refreshPriceRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly ISpecifyOpponentsService _specifyOpponentsService;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        IRefreshRequestRepository refreshRequestRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IParticipateService participateService,
        ISpecifyOpponentsService specifyOpponentsService,
        IRefreshPriceRepository refreshPriceRepo,
        IRankingRepository rankingRepo,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _refreshRequestRepo = refreshRequestRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
        _specifyOpponentsService = specifyOpponentsService;
        _refreshPriceRepo = refreshPriceRepo;
        _rankingRepo = rankingRepo;
        _jobClient = jobClient;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "AvailableOpponents",
        typeof(AvailableOpponentsResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable")]
    public async Task<
        Results<
            UnauthorizedHttpResult,
            NotFound<string>,
            StatusCodeHttpResult,
            Ok<AvailableOpponentsResponse>
        >
    > GetAvailableOpponents()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        var participant = await _participateService.ParticipateAsync(
            currentSeason.Value.Id,
            avatarAddress,
            query =>
                query
                    .Include(p => p.User)
                    .Include(p => p.RefreshRequest)
                    .ThenInclude(r => r.AvailableOpponents)
                    .ThenInclude(ao => ao.Opponent)
                    .ThenInclude(p => p.User)
                    .Include(p => p.RefreshRequest)
                    .ThenInclude(r => r.AvailableOpponents)
                    .ThenInclude(ao => ao.BattleLog)
        );

        if (participant.LastRefreshRequestId is null)
        {
            return TypedResults.NotFound("Not found");
        }

        if (participant.RefreshRequest.RoundId != currentRound.Value.Id)
        {
            return TypedResults.NotFound("No available opponents found for the current round.");
        }

        var availableOpponents = participant.RefreshRequest.AvailableOpponents;

        if (availableOpponents == null)
        {
            return TypedResults.NotFound("No available opponents found for the current round.");
        }

        var availableOpponentsResponses = new List<AvailableOpponentResponse>();

        foreach (var availableOpponent in availableOpponents)
        {
            var opponentRank = await _rankingRepo.GetRankAsync(
                new Address(availableOpponent.Opponent.AvatarAddress),
                currentSeason.Value.Id
            );
            availableOpponentsResponses.Add(
                new AvailableOpponentResponse
                {
                    AvatarAddress = availableOpponent.Opponent.AvatarAddress,
                    NameWithHash = availableOpponent.Opponent.User.NameWithHash,
                    PortraitId = availableOpponent.Opponent.User.PortraitId,
                    Cp = availableOpponent.Opponent.User.Cp,
                    Level = availableOpponent.Opponent.User.Level,
                    SeasonId = availableOpponent.Opponent.SeasonId,
                    Score = availableOpponent.Opponent.Score,
                    Rank = opponentRank,
                    IsAttacked = availableOpponent.BattleLogId is not null,
                    ScoreGainOnWin = OpponentGroupConstants
                        .Groups[availableOpponent.GroupId]
                        .WinScore,
                    ScoreLossOnLose = OpponentGroupConstants
                        .Groups[availableOpponent.GroupId]
                        .LoseScore,
                    IsVictory = availableOpponent.BattleLog?.IsVictory,
                    ClanImageURL = ""
                }
            );
        }

        return TypedResults.Ok(
            new AvailableOpponentsResponse
            {
                AvailableOpponents = availableOpponentsResponses,
                RefreshRequestId = participant.RefreshRequest.Id,
                RefreshStatus = participant.RefreshRequest.RefreshStatus,
            }
        );
    }

    [HttpGet("refresh/{refreshRequestId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "RefreshRequestResponse",
        typeof(RefreshRequestResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Status403Forbidden")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound")]
    public async Task<
        Results<
            UnauthorizedHttpResult,
            NotFound<string>,
            StatusCodeHttpResult,
            Ok<RefreshRequestResponse>
        >
    > GetRefreshRequestResult(int refreshRequestId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var refreshRequest = await _refreshRequestRepo.GetRefreshRequestByIdAsync(refreshRequestId);

        if (refreshRequest == null)
        {
            return TypedResults.NotFound("Status404NotFound");
        }

        if (refreshRequest.AvatarAddress != avatarAddress.ToHex())
        {
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(
            new RefreshRequestResponse
            {
                SpecifiedOpponentAvatarAddresses = refreshRequest.SpecifiedOpponentAvatarAddresses,
                RefreshStatus = refreshRequest.RefreshStatus,
                TxStatus = refreshRequest.TxStatus,
                TxId = refreshRequest.TxId,
            }
        );
    }

    [HttpPost("refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "RefreshRequest Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]
    [SwaggerResponse(StatusCodes.Status422UnprocessableEntity, "MaxAttemptsReached")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable")]
    public async Task<Results<StatusCodeHttpResult, Ok<int>>> RequestRefresh(string txId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        await _participateService.ParticipateAsync(currentSeason.Value.Id, avatarAddress);

        var refreshRequests = await _refreshRequestRepo.GetRefreshRequests(
            avatarAddress,
            currentRound.Value.Id
        );

        var refreshRequestsCount = await _refreshRequestRepo.GetRefreshRequestCount(
            avatarAddress,
            currentRound.Value.Id
        );

        RefreshPriceMaterializedView refreshPrice;
        try
        {
            refreshPrice = await _refreshPriceRepo.GetPriceAsync(
                currentSeason.Value.Id,
                refreshRequestsCount
            );
        }
        catch (InvalidOperationException)
        {
            return TypedResults.StatusCode(StatusCodes.Status422UnprocessableEntity);
        }

        if (refreshPrice.Price == 0)
        {
            return TypedResults.StatusCode(StatusCodes.Status422UnprocessableEntity);
        }

        var refreshRequest = await _refreshRequestRepo.AddRefreshRequest(
            currentSeason.Value.Id,
            currentSeason.Value.Id,
            avatarAddress,
            refreshPrice.DetailId,
            false,
            RefreshStatus.TRACKING,
            TxId.FromHex(txId),
            null,
            null
        );

        _jobClient.Enqueue<RefreshProcessor>(processor =>
            processor.ProcessAsync(refreshRequest.Id, TxId.FromHex(txId))
        );

        return TypedResults.Ok(refreshRequest.Id);
    }

    [HttpPost("free-refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "AvailableOpponents",
        typeof(AvailableOpponentsResponse)
    )]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "Free refresh is not available at this time. Additional cost is required."
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<
            NotFound<string>,
            StatusCodeHttpResult,
            BadRequest<string>,
            Ok<AvailableOpponentsResponse>
        >
    > RequestFreeRefresh()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var participant = await _participateService.ParticipateAsync(
            currentSeason.Value.Id,
            avatarAddress,
            query => query.Include(p => p.User)
        );

        var refreshRequestsCount = await _refreshRequestRepo.GetRefreshRequestCount(
            avatarAddress,
            currentRound.Value.Id
        );
        var refreshPrice = await _refreshPriceRepo.GetPriceAsync(
            currentSeason.Value.Id,
            refreshRequestsCount
        );

        if (refreshPrice.Price != 0)
        {
            return TypedResults.BadRequest(
                "Free refresh is not available at this time. Additional cost is required."
            );
        }

        var opponents = await _specifyOpponentsService.SpecifyOpponentsAsync(
            avatarAddress,
            currentSeason.Value.Id,
            currentRound.Value.Id
        );

        var refreshRequest = await _refreshRequestRepo.AddRefreshRequest(
            currentSeason.Value.Id,
            currentRound.Value.Id,
            avatarAddress,
            refreshPrice.DetailId,
            true,
            RefreshStatus.SUCCESS,
            null,
            null,
            opponents.Select(o => o.AvatarAddress).ToList()
        );
        await _availableOpponentRepo.AddAvailableOpponents(
            currentSeason.Value.Id,
            currentRound.Value.Id,
            avatarAddress,
            refreshRequest.Id,
            opponents.Select(o => (o.AvatarAddress, o.GroupId)).ToList()
        );
        await _participantRepo.UpdateLastRefreshRequestId(
            currentSeason.Value.Id,
            avatarAddress,
            refreshRequest.Id
        );

        var availableOpponentsResponses = new List<AvailableOpponentResponse>();

        foreach (var opponent in opponents)
        {
            var opponentParticipant = await _participantRepo.GetParticipantAsync(
                currentSeason.Value.Id,
                opponent.AvatarAddress,
                query => query.Include(p => p.User)
            );

            availableOpponentsResponses.Add(
                new AvailableOpponentResponse
                {
                    AvatarAddress = opponentParticipant.AvatarAddress,
                    NameWithHash = opponentParticipant.User.NameWithHash,
                    PortraitId = opponentParticipant.User.PortraitId,
                    Cp = opponentParticipant.User.Cp,
                    Level = opponentParticipant.User.Level,
                    SeasonId = opponentParticipant.SeasonId,
                    Score = opponentParticipant.Score,
                    Rank = opponent.Rank,
                    IsAttacked = false,
                    ScoreGainOnWin = OpponentGroupConstants.Groups[opponent.GroupId].WinScore,
                    ScoreLossOnLose = OpponentGroupConstants.Groups[opponent.GroupId].LoseScore,
                    IsVictory = null,
                    ClanImageURL = ""
                }
            );
        }

        return TypedResults.Ok(
            new AvailableOpponentsResponse
            {
                AvailableOpponents = availableOpponentsResponses,
                RefreshRequestId = participant.RefreshRequest.Id,
                RefreshStatus = refreshRequest.RefreshStatus,
            }
        );
    }
}
