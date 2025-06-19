namespace ArenaService.Controllers;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Extensions;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IAllClanRankingRepository _allClanRankingRepo;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        ITicketRepository ticketRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IParticipateService participateService,
        IAllClanRankingRepository allClanRankingRepo,
        IRankingRepository rankingRepo
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _ticketRepo = ticketRepo;
        _participantRepo = participantRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
        _allClanRankingRepo = allClanRankingRepo;
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
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
    public async Task<IActionResult> GetAvailableOpponents()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var participant = await _participateService.ParticipateAsync(
            cachedSeason.Id,
            cachedRound.Id,
            cachedRound.RoundIndex,
            avatarAddress,
            (int)(cachedRound.EndBlock - cachedRound.StartBlock),
            query => query.Include(p => p.User)
        );

        var availableOpponents = await _availableOpponentRepo.GetAvailableOpponents(
            avatarAddress,
            cachedRound.Id,
            query =>
                query
                    .Include(ao => ao.Opponent)
                    .ThenInclude(p => p.User)
                    .ThenInclude(u => u.Clan)
                    .Include(ao => ao.SuccessBattle)
        );

        if (!availableOpponents.Any())
        {
            return NotFound("NO_OPPONENTS_AVAILABLE");
        }

        var availableOpponentsResponses = new List<AvailableOpponentResponse>();
        foreach (var availableOpponent in availableOpponents)
        {
            var opponentRank = await _rankingRepo.GetRankAsync(
                availableOpponent.Opponent.AvatarAddress,
                cachedSeason.Id,
                cachedRound.Id
            );
            var opponentScore = await _rankingRepo.GetScoreAsync(
                availableOpponent.Opponent.AvatarAddress,
                cachedSeason.Id,
                cachedRound.Id
            );

            ClanResponse? clanResponse = null;
            if (availableOpponent.Opponent.User.ClanId is not null)
            {
                var clanRank = await _allClanRankingRepo.GetRankAsync(
                    availableOpponent.Opponent.User.ClanId!.Value,
                    cachedSeason.Id,
                    cachedRound.Id
                );
                var clanScore = await _allClanRankingRepo.GetScoreAsync(
                    availableOpponent.Opponent.User.ClanId!.Value,
                    cachedSeason.Id,
                    cachedRound.Id
                );
                clanResponse = availableOpponent.Opponent.User.Clan!.ToResponse(
                    clanRank,
                    clanScore
                );
            }

            availableOpponentsResponses.Add(
                AvailableOpponentResponse.FromAvailableOpponent(
                    availableOpponent,
                    opponentRank,
                    opponentScore,
                    clanResponse
                )
            );
        }

        return Ok(availableOpponentsResponses.OrderBy(ao => ao.GroupId));
    }

    [HttpPost("refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "AvailableOpponents",
        typeof(List<AvailableOpponentResponse>)
    )]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Status400BadRequest", typeof(string))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status423Locked, "Status423Locked", typeof(string))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(string))]
    public async Task<IActionResult> RequestFreeRefresh()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        if (
            cachedRound.EndBlock - ArenaServiceConfig.USE_TICKET_BLOCK_THRESHOLD
            <= cachedBlockIndex
        )
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                "ROUND_ENDING"
            );
        }

        var participant = await _participateService.ParticipateAsync(
            cachedSeason.Id,
            cachedRound.Id,
            cachedRound.RoundIndex,
            avatarAddress,
            (int)(cachedRound.EndBlock - cachedRound.StartBlock),
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
            return BadRequest("NO_REFRESH_TICKETS");
        }

        var myScore = await _rankingRepo.GetScoreAsync(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );

        var opponents = await _rankingRepo.SelectBattleOpponentsAsync(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id,
            cachedSeason.StartBlock == cachedRound.StartBlock
        );

        var availableOpponents = await _availableOpponentRepo.RefreshAvailableOpponents(
            cachedSeason.Id,
            cachedRound.Id,
            avatarAddress,
            opponents.Select(o => (o.Value.AvatarAddress, o.Key)).ToList()
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

        foreach (var (groupId, opponent) in opponents)
        {
            var opponentParticipant = await _participantRepo.GetParticipantAsync(
                cachedSeason.Id,
                opponent.AvatarAddress,
                query => query.Include(p => p.User).ThenInclude(u => u.Clan)
            );

            var opponentRank = await _rankingRepo.GetRankAsync(
                opponentParticipant!.AvatarAddress,
                cachedSeason.Id,
                cachedRound.Id
            );

            ClanResponse? clanResponse = null;
            if (opponentParticipant.User.ClanId is not null)
            {
                var clanRank = await _allClanRankingRepo.GetRankAsync(
                    opponentParticipant.User.ClanId!.Value,
                    cachedSeason.Id,
                    cachedRound.Id
                );
                var clanScore = await _allClanRankingRepo.GetScoreAsync(
                    opponentParticipant.User.ClanId!.Value,
                    cachedSeason.Id,
                    cachedRound.Id
                );
                clanResponse = opponentParticipant.User.Clan!.ToResponse(clanRank, clanScore);
            }

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
                    GroupId = groupId,
                    Rank = opponentRank,
                    IsAttacked = false,
                    ScoreGainOnWin = OpponentGroupConstants.Groups[groupId].WinScore,
                    ScoreLossOnLose = OpponentGroupConstants.Groups[groupId].LoseScore,
                    IsVictory = null,
                    ClanInfo = clanResponse
                }
            );
        }

        return Ok(availableOpponentsResponses.OrderBy(ao => ao.GroupId));
    }
}
