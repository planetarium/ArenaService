namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
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

    [HttpGet("current")]
    public async Task<Results<NotFound<string>, Ok<SeasonResponse>>> GetCurrentSeason(
        int blockIndex
    )
    {
        var seasons = await _seasonRepo.GetActivatedSeasonsAsync();
        var currentSeason = seasons.FirstOrDefault(s =>
            s.StartBlockIndex <= blockIndex && s.EndBlockIndex >= blockIndex
        );

        if (currentSeason == null)
        {
            return TypedResults.NotFound("No active season found.");
        }

        return TypedResults.Ok(currentSeason?.ToResponse());
    }

    [HttpPost("/{id}")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "ES256K")]
    public async Task<Results<NotFound<string>, Ok>> AddSeason()
    {
        return TypedResults.Ok();
    }
}
