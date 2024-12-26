namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/leaderboard")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardRepository _leaderboardRepository;

    public LeaderboardController(ILeaderboardRepository leaderboardRepository)
    {
        _leaderboardRepository = leaderboardRepository;
    }

    // [HttpGet]
    // public async Task<Results<NotFound<string>, Ok<AvailableOpponentsResponse>>> GetLeaderboard(
    //     int seasonId,
    //     int offset,
    //     int limit
    // )
    // {
    //     var leaderboard = await _leaderboardRepository.GetLeaderboard(seasonId, offset, limit);
    // }

    [HttpGet("participants/{participantId}")]
    public async Task<Results<NotFound<string>, Ok<LeaderboardEntryResponse>>> GetMyRank(
        int seasonId,
        int participantId
    )
    {
        var leaderboardEntry = await _leaderboardRepository.GetMyRankAsync(seasonId, participantId);

        if (leaderboardEntry == null)
        {
            return TypedResults.NotFound("No leaderboardEntry found.");
        }

        return TypedResults.Ok(leaderboardEntry.ToResponse());
    }
}
