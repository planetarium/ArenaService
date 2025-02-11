namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("info")]
[ApiController]
public class ArenaInfoController : ControllerBase
{
    private readonly IParticipantRepository _participantRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IClanRankingRepository _clanRankingRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;

    public ArenaInfoController(
        IParticipantRepository participantRepo,
        ITicketRepository ticketRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        _participantRepo = participantRepo;
        _ticketRepo = ticketRepo;
        _rankingRepo = rankingRepo;
        _clanRankingRepo = clanRankingRepo;
        _seasonCacheRepo = seasonCacheRepo;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "ArenaInfoResponse", typeof(ArenaInfoResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound")]
    public async Task<IActionResult> GetArenaInfo()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var participant = await _participantRepo.GetParticipantAsync(
            cachedSeason.Id,
            avatarAddress,
            q =>
                q.Include(p => p.User)
                    .Include(p => p.Season)
                    .ThenInclude(s => s.BattleTicketPolicy)
                    .Include(p => p.Season)
                    .ThenInclude(s => s.RefreshTicketPolicy)
                    .Include(p => p.User)
                    .ThenInclude(u => u.Clan)
        );

        if (participant is null)
        {
            return NotFound("not found");
        }

        ClanResponse? myClanResponse = null;
        if (participant.User.Clan is not null)
        {
            var myClanRank = await _clanRankingRepo.GetRankAsync(
                participant.User.ClanId!.Value,
                cachedSeason.Id,
                cachedRound.Id
            );
            var myClanScore = await _clanRankingRepo.GetScoreAsync(
                participant.User.ClanId!.Value,
                cachedSeason.Id,
                cachedRound.Id
            );
            myClanResponse = new ClanResponse
            {
                ImageURL = participant.User!.Clan!.ImageURL,
                Name = participant.User!.Clan!.Name,
                Rank = myClanRank,
                Score = myClanScore,
            };
        }

        var battleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            cachedSeason.Id,
            avatarAddress
        );
        var battleTicketStatusPerRound = await _ticketRepo.GetBattleTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );

        BattleTicketStatusResponse battleTicketStatus;
        if (battleTicketStatusPerSeason is null && battleTicketStatusPerRound is null)
        {
            battleTicketStatus = BattleTicketStatusResponse.CreateBattleTicketDefault(participant.Season);
        }
        else if (battleTicketStatusPerRound is null && battleTicketStatusPerSeason is not null)
        {
            battleTicketStatus = BattleTicketStatusResponse.CreateBattleTicketDefault(
                participant.Season,
                battleTicketStatusPerSeason
            );
        }
        else
        {
            battleTicketStatus = BattleTicketStatusResponse.FromBattleStatusModels(
                battleTicketStatusPerSeason!,
                battleTicketStatusPerRound!
            );
        }

        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );

        RefreshTicketStatusResponse refreshTicketStatus;
        if (refreshTicketStatusPerRound is null)
        {
            refreshTicketStatus = RefreshTicketStatusResponse.CreateRefreshTicketDefault(
                participant.Season
            );
        }
        else
        {
            refreshTicketStatus = RefreshTicketStatusResponse.FromRefreshStatusModel(
                refreshTicketStatusPerRound
            );
        }

        var score = await _rankingRepo.GetScoreAsync(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );
        var rank = await _rankingRepo.GetRankAsync(avatarAddress, cachedSeason.Id, cachedRound.Id);
        var nextScore = await _rankingRepo.GetScoreAsync(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id + 1
        );
        var nextRank = await _rankingRepo.GetRankAsync(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id + 1
        );

        var arenaInfo = new ArenaInfoResponse
        {
            SeasonId = cachedSeason.Id,
            RoundId = cachedRound.Id,
            ClanInfo = myClanResponse,
            User = participant.User.ToResponse(),
            Score = score,
            Rank = rank,
            CurrentRoundScoreChange = nextScore - score,
            CurrentRoundRankChange = nextRank - rank,
            TotalWin = participant.TotalWin,
            TotalLose = participant.TotalLose,
            CurrentRoundWinChange = battleTicketStatusPerRound is null
                ? 0
                : battleTicketStatusPerRound.WinCount,
            CurrentRoundLoseChange = battleTicketStatusPerRound is null
                ? 0
                : battleTicketStatusPerRound.LoseCount,
            BattleTicketStatus = battleTicketStatus,
            RefreshTicketStatus = refreshTicketStatus
        };
        return Ok(arenaInfo);
    }
}
