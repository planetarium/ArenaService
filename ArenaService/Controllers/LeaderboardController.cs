namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("leaderboard")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly IAllClanRankingRepository _allClanRankingRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly ILeaderboardRepository _leaderboardRepo;
    private readonly ISeasonService _seasonService;
    private readonly ISeasonCacheRepository _seasonCacheRepo;

    public LeaderboardController(
        IAllClanRankingRepository allClanRankingRepo,
        IRankingRepository rankingRepo,
        ILeaderboardRepository leaderboardRepo,
        ISeasonService seasonService,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        _allClanRankingRepo = allClanRankingRepo;
        _rankingRepo = rankingRepo;
        _leaderboardRepo = leaderboardRepo;
        _seasonService = seasonService;
        _seasonCacheRepo = seasonCacheRepo;
    }

    [HttpGet("count")]
    [SwaggerResponse(StatusCodes.Status200OK, "Ranking Count Response", typeof(int))]
    public async Task<ActionResult<int>> GetRankingCount(int seasonId, int roundIndex)
    {
        var rankingCount = await _rankingRepo.GetRankingCountAsync(seasonId, roundIndex);

        return Ok(rankingCount);
    }

    [HttpGet("completed")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Completed Arena Leaderboard Response",
        typeof(CompletedSeasonLeaderboardResponse)
    )]
    public async Task<
        ActionResult<CompletedSeasonLeaderboardResponse>
    > GetCompletedArenaLeaderboard(long blockIndex)
    {
        try
        {
            var seasonInfo = await _seasonService.GetSeasonAndRoundByBlock(blockIndex);
            var season = seasonInfo.Season;

            var currentSeasonInfo = await _seasonCacheRepo.GetSeasonAsync();
            
            if (blockIndex >= currentSeasonInfo.StartBlock)
            {
                return BadRequest(
                    "The requested block index corresponds to an ongoing or future season."
                );
            }

            var leaderboardData = await _leaderboardRepo.GetLeaderboardAsync(season.Id);

            var response = new CompletedSeasonLeaderboardResponse
            {
                Season = new SimpleSeasonResponse
                {
                    Id = season.Id,
                    SeasonGroupId = season.SeasonGroupId,
                    StartBlock = season.StartBlock,
                    EndBlock = season.EndBlock,
                    ArenaType = season.ArenaType,
                },
                Leaderboard = leaderboardData
                    .Select(item => new LeaderboardEntryResponse
                    {
                        Rank = item.Rank,
                        AvatarAddress = item.Participant.User.AvatarAddress.ToString().ToLower(),
                        AgentAddress = item.Participant.User.AgentAddress.ToString().ToLower(),
                        NameWithHash = item.Participant.User.NameWithHash,
                        Score = item.Score,
                        TotalWin = item.Participant.TotalWin,
                        TotalLose = item.Participant.TotalLose,
                        Level = item.Participant.User.Level,
                    })
                    .ToList(),
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
