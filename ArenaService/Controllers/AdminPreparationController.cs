namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("admin/preparation")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminPreparationController : ControllerBase
{
    private readonly ISeasonPreparationService _seasonPreparation;
    private readonly IRoundPreparationService _roundPreparation;
    private readonly ICacheInitializationService _cacheInitialization;
    private readonly ISeasonService _seasonService;

    public AdminPreparationController(
        ISeasonPreparationService seasonPreparation,
        IRoundPreparationService roundPreparation,
        ICacheInitializationService cacheInitialization,
        ISeasonService seasonService
    )
    {
        _seasonPreparation = seasonPreparation;
        _roundPreparation = roundPreparation;
        _cacheInitialization = cacheInitialization;
        _seasonService = seasonService;
    }

    [HttpPost("season/initialize")]
    [SwaggerResponse(StatusCodes.Status200OK, "Season initialized")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Season not found")]
    public async Task<IActionResult> InitializeSeason([FromBody] InitializeSeasonRequest request)
    {
        try
        {
            var seasonAndRound = await _seasonService.GetSeasonAndRoundByBlock(request.BlockIndex);
            await _seasonPreparation.PrepareSeasonAsync(seasonAndRound);
            return Ok(new { Message = "Season initialized successfully.", SeasonId = seasonAndRound.Season.Id, RoundId = seasonAndRound.Round.Id });
        }
        catch (NotFoundSeasonException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("round/prepare-next")]
    [SwaggerResponse(StatusCodes.Status200OK, "Round prepared")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Season/round not found")]
    public async Task<IActionResult> PrepareNextRound([FromBody] PrepareNextRoundRequest request)
    {
        try
        {
            var seasonAndRound = await _seasonService.GetSeasonAndRoundByBlock(request.BlockIndex);
            await _roundPreparation.PrepareNextRoundWithSnapshotAsync(seasonAndRound);
            return Ok(new { Message = "Next round prepared successfully.", SeasonId = seasonAndRound.Season.Id, RoundId = seasonAndRound.Round.Id });
        }
        catch (NotFoundSeasonException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("ranking-cache/initialize")]
    [SwaggerResponse(StatusCodes.Status200OK, "Ranking cache initialized")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Insufficient snapshots")]
    public async Task<IActionResult> InitializeRankingCache([FromBody] InitializeRankingCacheRequest request)
    {
        var result = await _cacheInitialization.InitializeRankingCacheAsync(request.SeasonId, request.RoundId);

        if (!result)
        {
            return BadRequest("Snapshot count is below participant count threshold. Cannot initialize ranking cache.");
        }

        return Ok(new { Message = "Ranking cache initialized successfully." });
    }
}
