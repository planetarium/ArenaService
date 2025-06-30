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

    public LeaderboardController(
        IAllClanRankingRepository allClanRankingRepo,
        IRankingRepository rankingRepo,
        ILeaderboardRepository leaderboardRepo,
        ISeasonService seasonService
    )
    {
        _allClanRankingRepo = allClanRankingRepo;
        _rankingRepo = rankingRepo;
        _leaderboardRepo = leaderboardRepo;
        _seasonService = seasonService;
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
        typeof(CompletedArenaLeaderboardResponse)
    )]
    public async Task<ActionResult<CompletedArenaLeaderboardResponse>> GetCompletedArenaLeaderboard(
        long blockIndex
    )
    {
        (Season Season, Round Round) currentSeasonInfo;
        try
        {
            currentSeasonInfo = await _seasonService.GetSeasonAndRoundByBlock(blockIndex);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        var currentSeason = currentSeasonInfo.Season;

        var completedSeasons = await _seasonService.GetCompletedSeasonsBeforeBlock(blockIndex);

        if (!completedSeasons.Any())
        {
            return Ok(
                new CompletedArenaLeaderboardResponse
                {
                    CurrentSeason = new SimpleSeasonResponse
                    {
                        Id = currentSeason.Id,
                        SeasonGroupId = currentSeason.SeasonGroupId,
                        StartBlock = currentSeason.StartBlock,
                        EndBlock = currentSeason.EndBlock,
                        ArenaType = currentSeason.ArenaType,
                    },
                    CompletedSeasons = new List<CompletedSeasonLeaderboardResponse>(),
                }
            );
        }

        var completedSeasonLeaderboards = new List<CompletedSeasonLeaderboardResponse>();

        foreach (var season in completedSeasons)
        {
            var leaderboardData = await _leaderboardRepo.GetLeaderboardAsync(season.Id);

            var leaderboardResponse = new CompletedSeasonLeaderboardResponse
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

            completedSeasonLeaderboards.Add(leaderboardResponse);
        }

        return Ok(
            new CompletedArenaLeaderboardResponse
            {
                CurrentSeason = new SimpleSeasonResponse
                {
                    Id = currentSeason.Id,
                    SeasonGroupId = currentSeason.SeasonGroupId,
                    StartBlock = currentSeason.StartBlock,
                    EndBlock = currentSeason.EndBlock,
                    ArenaType = currentSeason.ArenaType,
                },
                CompletedSeasons = completedSeasonLeaderboards,
            }
        );
    }
}
