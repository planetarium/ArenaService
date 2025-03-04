namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("leaderboard")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly IAllClanRankingRepository _allClanRankingRepo;
    private readonly IRankingRepository _rankingRepo;

    public LeaderboardController(
        IAllClanRankingRepository allClanRankingRepo,
        IRankingRepository rankingRepo
    )
    {
        _allClanRankingRepo = allClanRankingRepo;
        _rankingRepo = rankingRepo;
    }

    [HttpGet("count")]
    [SwaggerResponse(StatusCodes.Status200OK, "Ranking Count Response", typeof(int))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(ErrorResponse))]
    public async Task<ActionResult<int>> GetRankingCount(int seasonId, int roundId)
    {
        var rankingCount = await _rankingRepo.GetRankingCountAsync(seasonId, roundId);

        if (rankingCount == 0)
        {
            return NotFound(
                new ErrorResponse(
                    "NO_RANKINGS",
                    $"No rankings found for season {seasonId} and round {roundId}"
                )
            );
        }

        return Ok(rankingCount);
    }
}
