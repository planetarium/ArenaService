namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("clans")]
[ApiController]
public class ClanController : ControllerBase
{
    public ClanController() { }

    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(ClanLeaderboardResponse), StatusCodes.Status200OK)]
    public async Task<Ok<List<SeasonResponse>>> GetClanLeaderboard(int offset = 0, int limit = 100)
    {
        return TypedResults.Ok(new List<SeasonResponse>());
    }

    [HttpGet]
    [ProducesResponseType(typeof(ClanResponse), StatusCodes.Status200OK)]
    public async Task<Ok> GetMyClan()
    {
        return TypedResults.Ok();
    }
}
