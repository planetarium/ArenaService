namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons")]
[ApiController]
public class SeasonController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepo;

    public SeasonController(ISeasonRepository seasonRepo)
    {
        _seasonRepo = seasonRepo;
    }

    [HttpGet("by-block/{blockIndex}")]
    [ProducesResponseType(typeof(SeasonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<Results<NotFound<string>, Ok<SeasonResponse>>> GetSeasonByBlock(
        int blockIndex
    )
    {
        var seasons = await _seasonRepo.GetAllSeasonsAsync();
        var season = seasons.FirstOrDefault(s =>
            s.StartBlock <= blockIndex && s.EndBlock >= blockIndex
        );

        if (season == null)
        {
            return TypedResults.NotFound($"No active season found for block index {blockIndex}.");
        }

        return TypedResults.Ok(season?.ToResponse());
    }
}
