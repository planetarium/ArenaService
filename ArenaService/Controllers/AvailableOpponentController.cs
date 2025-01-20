namespace ArenaService.Controllers;

using ArenaService.Constants;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Services;
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
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IParticipateService _participateService;
    private readonly IRankingRepository _rankingRepo;
    private readonly ISpecifyOpponentsService _specifyOpponentsService;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IParticipateService participateService,
        ISpecifyOpponentsService specifyOpponentsService,
        IRankingRepository rankingRepo,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
        _specifyOpponentsService = specifyOpponentsService;
        _rankingRepo = rankingRepo;
        _jobClient = jobClient;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "AvailableOpponents",
        typeof(List<AvailableOpponentResponse>)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable")]
    public async Task<
        Results<
            UnauthorizedHttpResult,
            NotFound<string>,
            StatusCodeHttpResult,
            Ok
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
        );

        var availableOpponentsResponses = new List<AvailableOpponentResponse>();

        // foreach (var availableOpponent in availableOpponents)
        // {
        //     var opponentRank = await _rankingRepo.GetRankAsync(
        //         new Address(availableOpponent.Opponent.AvatarAddress),
        //         currentSeason.Value.Id
        //     );
        //     availableOpponentsResponses.Add(
        //         new AvailableOpponentResponse
        //         {
        //             AvatarAddress = availableOpponent.Opponent.AvatarAddress,
        //             NameWithHash = availableOpponent.Opponent.User.NameWithHash,
        //             PortraitId = availableOpponent.Opponent.User.PortraitId,
        //             Cp = availableOpponent.Opponent.User.Cp,
        //             Level = availableOpponent.Opponent.User.Level,
        //             SeasonId = availableOpponent.Opponent.SeasonId,
        //             Score = availableOpponent.Opponent.Score,
        //             Rank = opponentRank,
        //             IsAttacked = availableOpponent.BattleId is not null,
        //             ScoreGainOnWin = OpponentGroupConstants
        //                 .Groups[availableOpponent.GroupId]
        //                 .WinScore,
        //             ScoreLossOnLose = OpponentGroupConstants
        //                 .Groups[availableOpponent.GroupId]
        //                 .LoseScore,
        //             IsVictory = availableOpponent.Battle?.IsVictory,
        //             ClanImageURL = ""
        //         }
        //     );
        // }

        return TypedResults.Ok();
    }

    [HttpPost("refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "AvailableOpponents",
        typeof(List<AvailableOpponentResponse>)
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
            Ok
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

        var opponents = await _specifyOpponentsService.SpecifyOpponentsAsync(
            avatarAddress,
            currentSeason.Value.Id,
            currentRound.Value.Id
        );

        // await _availableOpponentRepo.AddAvailableOpponents(
        //     currentSeason.Value.Id,
        //     currentRound.Value.Id,
        //     avatarAddress,
        //     refreshRequest.Id,
        //     opponents.Select(o => (o.AvatarAddress, o.GroupId)).ToList()
        // );
        // await _participantRepo.UpdateLastRefreshRequestId(
        //     currentSeason.Value.Id,
        //     avatarAddress,
        //     refreshRequest.Id
        // );

        // var availableOpponentsResponses = new List<AvailableOpponentResponse>();

        // foreach (var opponent in opponents)
        // {
        //     var opponentParticipant = await _participantRepo.GetParticipantAsync(
        //         currentSeason.Value.Id,
        //         opponent.AvatarAddress,
        //         query => query.Include(p => p.User)
        //     );

        //     availableOpponentsResponses.Add(
        //         new AvailableOpponentResponse
        //         {
        //             AvatarAddress = opponentParticipant.AvatarAddress,
        //             NameWithHash = opponentParticipant.User.NameWithHash,
        //             PortraitId = opponentParticipant.User.PortraitId,
        //             Cp = opponentParticipant.User.Cp,
        //             Level = opponentParticipant.User.Level,
        //             SeasonId = opponentParticipant.SeasonId,
        //             Score = opponentParticipant.Score,
        //             Rank = opponent.Rank,
        //             IsAttacked = false,
        //             ScoreGainOnWin = OpponentGroupConstants.Groups[opponent.GroupId].WinScore,
        //             ScoreLossOnLose = OpponentGroupConstants.Groups[opponent.GroupId].LoseScore,
        //             IsVictory = null,
        //             ClanImageURL = ""
        //         }
        //     );
        // }

        return TypedResults.Ok();
    }
}
