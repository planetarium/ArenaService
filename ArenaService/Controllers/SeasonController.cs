namespace ArenaService.Controllers;

using ArenaService.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/seasons")]
[ApiController]
public class SeasonController : ControllerBase
{
    private readonly SeasonService _seasonService;

    public SeasonController(SeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSeason(int blockIndex)
    {
        var currentSeason = await _seasonService.GetCurrentSeasonAsync(blockIndex);

        if (currentSeason == null)
        {
            return NotFound("No active season found.");
        }

        return Ok(currentSeason);
    }
}
