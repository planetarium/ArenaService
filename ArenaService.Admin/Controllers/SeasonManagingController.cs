namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("manage-seasons")]
[ApiController]
public class SeasonManagingController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepo;
    private readonly IRoundRepository _roundRepo;
    private readonly ISeasonPreparationService _seasonPreparationService;
    private readonly IRoundPreparationService _roundPreparationService;

    public SeasonManagingController(
        ISeasonRepository seasonRepo,
        IRoundRepository roundRepo,
        ISeasonPreparationService seasonPreparationService,
        IRoundPreparationService roundPreparationService
    )
    {
        _seasonRepo = seasonRepo;
        _roundRepo = roundRepo;
        _seasonPreparationService = seasonPreparationService;
        _roundPreparationService = roundPreparationService;
    }

    [HttpPost("initialize-season")]
    [SwaggerResponse(StatusCodes.Status200OK, "OK")]
    public async Task<ActionResult> InitializeSeason(int seasonId)
    {
        var season = await _seasonRepo.GetSeasonAsync(seasonId, q => q.Include(s => s.Rounds));

        await _seasonPreparationService.PrepareSeasonAsync(
            (season, season.Rounds.OrderBy(r => r.StartBlock).First())
        );

        return Ok();
    }

    [HttpPost("prepare-next-round")]
    [SwaggerResponse(StatusCodes.Status200OK, "Ok")]
    public async Task<ActionResult> PrepareNextRound(int roundId)
    {
        var round = await _roundRepo.GetRoundAsync(roundId, q => q.Include(r => r.Season));

        await _roundPreparationService.PrepareNextRoundWithSnapshotAsync((round!.Season, round));

        return Ok();
    }
}
