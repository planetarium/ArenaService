namespace ArenaService.Controllers;

using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("admin/leaderboard")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminLeaderboardController : ControllerBase
{
    private readonly ILeaderboardRepository _leaderboardRepo;

    public AdminLeaderboardController(ILeaderboardRepository leaderboardRepo)
    {
        _leaderboardRepo = leaderboardRepo;
    }

    [HttpGet("{seasonId}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(int seasonId)
    {
        var leaderboard = await _leaderboardRepo.GetLeaderboardAsync(seasonId);

        var response = leaderboard.Select(entry => new
        {
            AvatarAddress = entry.Participant.AvatarAddress.ToString(),
            AgentAddress = entry.Participant.User.AgentAddress.ToString(),
            NameWithHash = entry.Participant.User.NameWithHash,
            Rank = entry.Rank,
            Score = entry.Score,
            TotalWin = entry.Participant.TotalWin,
            TotalLose = entry.Participant.TotalLose,
            Level = entry.Participant.User.Level
        });

        return Ok(response);
    }

    [HttpGet("{seasonId}/csv")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadLeaderboardCsv(int seasonId)
    {
        var csvBytes = await _leaderboardRepo.GenerateLeaderboardCsvAsync(seasonId);
        return File(csvBytes, "text/csv", $"leaderboard_season_{seasonId}.csv");
    }
}
