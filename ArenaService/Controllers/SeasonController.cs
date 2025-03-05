namespace ArenaService.Controllers;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Extensions;
using ArenaService.Options;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

[Route("seasons")]
[ApiController]
public class SeasonController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepo;
    private readonly ISeasonService _seasonService;
    private readonly Address _recipientAddress;

    public SeasonController(
        ISeasonRepository seasonRepo,
        ISeasonService seasonService,
        IOptions<OpsConfigOptions> options
    )
    {
        _seasonRepo = seasonRepo;
        _seasonService = seasonService;
        _recipientAddress = new Address(options.Value.RecipientAddress);
    }

    [HttpGet("by-block/{blockIndex}")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "SeasonAndRoundResponse",
        typeof(SeasonAndRoundResponse)
    )]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(string))]
    public async Task<ActionResult<SeasonAndRoundResponse>> GetSeasonAndRoundByBlock(
        long blockIndex
    )
    {
        try
        {
            var seasonInfo = await _seasonService.GetSeasonAndRoundByBlock(blockIndex);

            return Ok(seasonInfo.ToResponse());
        }
        catch (NotFoundSeasonException)
        {
            return NotFound("SEASON_NOT_FOUND");
        }
    }

    [HttpGet("classify-by-championship/{blockIndex}")]
    [SwaggerResponse(StatusCodes.Status200OK, "SeasonResponse", typeof(SeasonsResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(string))]
    public async Task<IActionResult> GetSeasons(long blockIndex)
    {
        var classifiedSeasons = await _seasonService.ClassifyByChampionship(
            blockIndex,
            q =>
                q.Include(s => s.BattleTicketPolicy)
                    .Include(s => s.RefreshTicketPolicy)
                    .Include(s => s.Rounds)
        );

        if (!classifiedSeasons.Any())
        {
            return NotFound("NO_SEASONS_FOUND");
        }

        var response = new SeasonsResponse
        {
            OperationAccountAddress = _recipientAddress,
            Seasons = classifiedSeasons.Select(s => s.ToResponse()).ToList()
        };

        return Ok(response);
    }
}
