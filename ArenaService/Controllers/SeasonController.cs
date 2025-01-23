namespace ArenaService.Controllers;

using ArenaService.Constants;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Options;
using ArenaService.Repositories;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

[Route("seasons")]
[ApiController]
public class SeasonController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly Address _recipientAddress;

    public SeasonController(
        ISeasonRepository seasonRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IOptions<OpsConfigOptions> options
    )
    {
        _seasonRepo = seasonRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _recipientAddress = new Address(options.Value.RecipientAddress);
    }

    [HttpGet("{seasonId}")]
    [ProducesResponseType(typeof(SeasonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeason(int seasonId)
    {
        return Ok(new SeasonResponse());
    }

    [HttpGet("by-block/{blockIndex}")]
    [ProducesResponseType(typeof(SeasonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeasonByBlock(long blockIndex)
    {
        var seasons = await _seasonRepo.GetAllSeasonsAsync();
        var season = seasons.FirstOrDefault(s =>
            s.StartBlock <= blockIndex && s.EndBlock >= blockIndex
        );

        if (season == null)
        {
            return NotFound($"No active season found for block index {blockIndex}.");
        }

        return Ok(season.ToResponse());
    }

    [HttpGet("classify-by-championship/{blockIndex}")]
    [ProducesResponseType(typeof(SeasonsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeasons(long blockIndex)
    {
        var seasons = await _seasonRepo.GetAllSeasonsAsync(q =>
            q.Include(s => s.Rounds)
                .Include(s => s.BattleTicketPolicy)
                .Include(s => s.RefreshTicketPolicy)
        );

        var currentSeason = seasons.FirstOrDefault(s =>
            s.StartBlock <= blockIndex && s.EndBlock >= blockIndex
        );

        if (currentSeason == null)
        {
            return Ok(
                new SeasonsResponse { OperationAccountAddress = _recipientAddress, Seasons = new() }
            );
        }

        var filteredSeasons = seasons
            .OrderBy(s => s.StartBlock)
            .SkipWhile(s => s.StartBlock < currentSeason.StartBlock)
            .ToList();

        var championshipIndex = filteredSeasons.FindIndex(s =>
            s.ArenaType == ArenaType.CHAMPIONSHIP
        );
        if (championshipIndex != -1)
        {
            filteredSeasons = filteredSeasons.Take(championshipIndex + 1).ToList();
        }

        var response = new SeasonsResponse
        {
            OperationAccountAddress = _recipientAddress,
            Seasons = filteredSeasons.Select(s => s.ToResponse()).ToList()
        };

        return Ok(response);
    }
}
