namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons")]
[ApiController]
public class SeasonController : ControllerBase
{
    private readonly SeasonService _seasonService;

    public SeasonController(SeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    [HttpGet("current")]
    public async Task<Results<NotFound<string>, Ok<SeasonResponse>>> GetCurrentSeason(
        int blockIndex
    )
    {
        var currentSeason = await _seasonService.GetCurrentSeasonAsync(blockIndex);

        if (currentSeason == null)
        {
            return TypedResults.NotFound("No active season found.");
        }

        return TypedResults.Ok(currentSeason);
    }
}
