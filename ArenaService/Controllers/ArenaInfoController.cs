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
    private readonly IBattleRepository _battleRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;

    public ArenaInfoController(
        IParticipantRepository participantRepo,
        IBattleRepository battleRepo,
        ITicketRepository ticketRepo,
        IRankingRepository rankingRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IAvailableOpponentRepository availableOpponentRepo
    )
    {
        _participantRepo = participantRepo;
        _battleRepo = battleRepo;
        _ticketRepo = ticketRepo;
        _rankingRepo = rankingRepo;
        _availableOpponentRepo = availableOpponentRepo;
        _seasonCacheRepo = seasonCacheRepo;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "ArenaInfoResponse", typeof(ArenaInfoResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound")]
    public async Task<Results<NotFound<string>, Ok<ArenaInfoResponse>>> GetArenaInfo()
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
        );

        if (participant is null)
        {
            return TypedResults.NotFound("not found");
        }

        var battleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            cachedSeason.Id,
            avatarAddress
        );
        var battleTicketStatusPerRound = await _ticketRepo.GetBattleTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );

        TicketStatusResponse battleTicketStatus;
        if (battleTicketStatusPerSeason is null || battleTicketStatusPerRound is null)
        {
            battleTicketStatus = TicketStatusResponse.CreateBattleTicketDefault(participant.Season);
        }
        else
        {
            battleTicketStatus = TicketStatusResponse.FromBattleStatusModels(
                battleTicketStatusPerSeason,
                battleTicketStatusPerRound
            );
        }

        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );

        TicketStatusResponse refreshTicketStatus;
        if (refreshTicketStatusPerRound is null)
        {
            refreshTicketStatus = TicketStatusResponse.CreateRefreshTicketDefault(
                participant.Season
            );
        }
        else
        {
            refreshTicketStatus = TicketStatusResponse.FromRefreshStatusModel(
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
            User = participant.User.ToResponse(),
            Score = score,
            Rank = rank,
            CurrentRoundScoreChange = nextScore - score,
            CurrentRoundRankChange = nextRank - rank,
            TotalWin = participant.TotalWin,
            TotalLose = participant.TotalLose,
            CurrentRoundWinChange = 0,
            CurrentRoundLoseChange = 0,
            BattleTicketStatus = battleTicketStatus,
            RefreshTicketStatus = refreshTicketStatus
        };
        return TypedResults.Ok(arenaInfo);
    }
}
