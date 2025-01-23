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
    private readonly ITicketRepository _ticketRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IParticipateService _participateService;
    private readonly IRankingRepository _rankingRepo;
    private readonly ISpecifyOpponentsService _specifyOpponentsService;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        ITicketRepository ticketRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IParticipateService participateService,
        ISpecifyOpponentsService specifyOpponentsService,
        IRankingRepository rankingRepo
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _ticketRepo = ticketRepo;
        _participantRepo = participantRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
        _specifyOpponentsService = specifyOpponentsService;
        _rankingRepo = rankingRepo;
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
    public async Task<IActionResult> GetAvailableOpponents()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var participant = await _participateService.ParticipateAsync(
            cachedSeason.Id,
            cachedRound.Id,
            avatarAddress,
            query => query.Include(p => p.User)
        );

        var availableOpponents = await _availableOpponentRepo.GetAvailableOpponents(
            avatarAddress,
            cachedRound.Id,
            query =>
                query
                    .Include(ao => ao.Opponent)
                    .ThenInclude(p => p.User)
                    .Include(ao => ao.SuccessBattle)
        );

        if (!availableOpponents.Any())
        {
            return NotFound("Refresh first");
        }

        var availableOpponentsResponses = new List<AvailableOpponentResponse>();
        foreach (var availableOpponent in availableOpponents)
        {
            var opponentRank = await _rankingRepo.GetRankAsync(
                availableOpponent.Opponent.AvatarAddress,
                cachedSeason.Id,
                cachedRound.Id
            );
            var opponentScore = await _rankingRepo.GetRankAsync(
                availableOpponent.Opponent.AvatarAddress,
                cachedSeason.Id,
                cachedRound.Id
            );
            availableOpponentsResponses.Add(
                AvailableOpponentResponse.FromAvailableOpponent(
                    availableOpponent,
                    opponentRank,
                    opponentScore
                )
            );
        }

        return Ok(availableOpponentsResponses);
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
    public async Task<IActionResult> RequestFreeRefresh()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var participant = await _participateService.ParticipateAsync(
            cachedSeason.Id,
            cachedRound.Id,
            avatarAddress,
            query =>
                query
                    .Include(p => p.User)
                    .Include(p => p.Season)
                    .ThenInclude(s => s.RefreshTicketPolicy)
        );

        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );

        if (refreshTicketStatusPerRound is null)
        {
            refreshTicketStatusPerRound = await _ticketRepo.AddRefreshTicketStatusPerRound(
                cachedSeason.Id,
                cachedRound.Id,
                avatarAddress,
                participant.Season.RefreshTicketPolicyId,
                participant.Season.RefreshTicketPolicy.DefaultTicketsPerRound,
                0,
                0
            );
        }

        if (refreshTicketStatusPerRound.RemainingCount <= 0)
        {
            return BadRequest("RemainingCount 0");
        }

        var opponents = await _specifyOpponentsService.SpecifyOpponentsAsync(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );

        var availableOpponents = await _availableOpponentRepo.RefreshAvailableOpponents(
            cachedSeason.Id,
            cachedRound.Id,
            avatarAddress,
            opponents.Select(o => (o.AvatarAddress, o.GroupId)).ToList()
        );

        await _ticketRepo.UpdateRefreshTicketStatusPerRound(
            refreshTicketStatusPerRound,
            rts =>
            {
                rts.RemainingCount -= 1;
                rts.UsedCount += 1;
            }
        );

        await _ticketRepo.AddRefreshTicketUsageLog(
            refreshTicketStatusPerRound.Id,
            availableOpponents.Select(o => o.Id).ToList()
        );

        var availableOpponentsResponses = new List<AvailableOpponentResponse>();

        foreach (var opponent in opponents)
        {
            var opponentParticipant = await _participantRepo.GetParticipantAsync(
                cachedSeason.Id,
                opponent.AvatarAddress,
                query => query.Include(p => p.User)
            );

            availableOpponentsResponses.Add(
                new AvailableOpponentResponse
                {
                    AvatarAddress = opponentParticipant!.AvatarAddress,
                    NameWithHash = opponentParticipant.User.NameWithHash,
                    PortraitId = opponentParticipant.User.PortraitId,
                    Cp = opponentParticipant.User.Cp,
                    Level = opponentParticipant.User.Level,
                    SeasonId = opponentParticipant.SeasonId,
                    Score = opponent.Score,
                    Rank = opponent.Rank,
                    IsAttacked = false,
                    ScoreGainOnWin = OpponentGroupConstants.Groups[opponent.GroupId].WinScore,
                    ScoreLossOnLose = OpponentGroupConstants.Groups[opponent.GroupId].LoseScore,
                    IsVictory = null,
                    ClanImageURL = ""
                }
            );
        }

        return Ok(availableOpponentsResponses);
    }
}
