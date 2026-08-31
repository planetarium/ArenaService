using ArenaService.BackOffice.Authentication;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.BackOffice.Controllers;

/// <summary>
/// Season / round management. Mirrors the ManageSeasons Blazor page.
/// </summary>
[ApiController]
[Route("api/seasons")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly ISeasonCacheRepository _seasonCacheRepository;
    private readonly ISeasonBlockAdjustmentService _seasonBlockAdjustmentService;
    private readonly ISeasonService _seasonService;
    private readonly ILogger<SeasonsController> _logger;

    public SeasonsController(
        ISeasonRepository seasonRepository,
        ISeasonCacheRepository seasonCacheRepository,
        ISeasonBlockAdjustmentService seasonBlockAdjustmentService,
        ISeasonService seasonService,
        ILogger<SeasonsController> logger
    )
    {
        _seasonRepository = seasonRepository;
        _seasonCacheRepository = seasonCacheRepository;
        _seasonBlockAdjustmentService = seasonBlockAdjustmentService;
        _seasonService = seasonService;
        _logger = logger;
    }

    /// <summary>Gets a page of seasons with their rounds and ticket policies.</summary>
    /// <remarks><c>deletable</c> reflects whether the season starts after the cached block index.</remarks>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SeasonDto>>>> GetSeasons(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        if (page < 1 || pageSize < 1)
        {
            return BadRequest(
                ApiResponse<PagedResult<SeasonDto>>.Error("page and pageSize must be positive.")
            );
        }

        var seasons = await _seasonRepository.GetSeasonsPagedAsync(
            page,
            pageSize,
            q => q.Include(s => s.Rounds).Include(s => s.BattleTicketPolicy).Include(s => s.RefreshTicketPolicy)
        );
        var totalCount = await _seasonRepository.GetTotalSeasonsCountAsync();

        long? currentBlockIndex = null;
        try
        {
            currentBlockIndex = await _seasonCacheRepository.GetBlockIndexAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read the cached block index; deletable will be null.");
        }

        var items = seasons
            .Select(s => SeasonDto.From(s, currentBlockIndex is null ? null : s.StartBlock >= currentBlockIndex))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return Ok(
            ApiResponse<PagedResult<SeasonDto>>.Ok(
                new PagedResult<SeasonDto>(items, page, pageSize, totalCount, totalPages)
            )
        );
    }

    /// <summary>Gets a single season with its rounds.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SeasonDto>>> GetSeason(int id)
    {
        try
        {
            var season = await _seasonRepository.GetSeasonAsync(id, q => q.Include(s => s.Rounds));
            return Ok(ApiResponse<SeasonDto>.Ok(SeasonDto.From(season)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Season {SeasonId} lookup failed.", id);
            return NotFound(ApiResponse<SeasonDto>.Error($"Season {id} not found."));
        }
    }

    /// <summary>Gets the block the next season should start at (last season end block + 1).</summary>
    [HttpGet("next-start-block")]
    public async Task<ActionResult<ApiResponse<long>>> GetNextStartBlock()
    {
        var lastEndBlock = await _seasonRepository.GetLastSeasonEndBlockAsync();
        return Ok(ApiResponse<long>.Ok(lastEndBlock + 1 ?? 1));
    }

    /// <summary>Creates a season together with its rounds.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SeasonDto>>> AddSeason([FromBody] AddSeasonRequest request)
    {
        var endBlock = request.StartBlock + (long)request.RoundInterval * request.RoundCount - 1;
        if (await _seasonRepository.IsBlockRangeOverlappingAsync(request.StartBlock, endBlock))
        {
            return Conflict(
                ApiResponse<SeasonDto>.Error(
                    $"Block range {request.StartBlock}-{endBlock} overlaps an existing season."
                )
            );
        }

        try
        {
            var season = await _seasonRepository.AddSeasonWithRoundsAsync(
                request.StartBlock,
                request.RoundInterval,
                request.RoundCount,
                request.SeasonGroupId,
                request.ArenaType,
                request.RequiredMedalCount,
                request.TotalPrize,
                request.BattleTicketPolicyId,
                request.RefreshTicketPolicyId
            );
            return Ok(ApiResponse<SeasonDto>.Ok(SeasonDto.From(season), "Season created."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add season.");
            return StatusCode(500, ApiResponse<SeasonDto>.Error($"Failed to add season: {ex.Message}"));
        }
    }

    /// <summary>Updates the editable metadata of a season. Block ranges are untouched.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<SeasonDto>>> UpdateSeason(
        int id,
        [FromBody] UpdateSeasonRequest request
    )
    {
        try
        {
            var season = await _seasonRepository.UpdateSeasonAsync(
                id,
                request.SeasonGroupId,
                request.ArenaType,
                request.RoundInterval,
                request.RequiredMedalCount,
                request.TotalPrize,
                request.BattleTicketPolicyId,
                request.RefreshTicketPolicyId
            );
            return Ok(ApiResponse<SeasonDto>.Ok(SeasonDto.From(season), "Season updated."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update season {SeasonId}.", id);
            return StatusCode(500, ApiResponse<SeasonDto>.Error($"Failed to update season: {ex.Message}"));
        }
    }

    /// <summary>Moves the end block of a season, re-aligning its rounds.</summary>
    [HttpPost("{id:int}/end-block")]
    public async Task<ActionResult<ApiResponse<SeasonDto>>> AdjustEndBlock(
        int id,
        [FromBody] AdjustEndBlockRequest request
    )
    {
        try
        {
            var season = await _seasonBlockAdjustmentService.AdjustSeasonEndBlockAsync(
                id,
                request.NewEndBlock
            );
            return Ok(ApiResponse<SeasonDto>.Ok(SeasonDto.From(season), "Season end block adjusted."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adjust end block of season {SeasonId}.", id);
            return StatusCode(
                500,
                ApiResponse<SeasonDto>.Error($"Failed to adjust season end block: {ex.Message}")
            );
        }
    }

    /// <summary>Deletes a season that has not started yet.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteSeason(int id)
    {
        try
        {
            var currentBlockIndex = await _seasonCacheRepository.GetBlockIndexAsync();
            if (!await _seasonService.CanDeleteSeasonAsync(id, currentBlockIndex))
            {
                return Conflict(
                    ApiResponse.Error($"Season {id} has already started and cannot be deleted.")
                );
            }

            await _seasonService.DeleteSeasonAsync(id);
            return Ok(ApiResponse.Ok("Season deleted."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete season {SeasonId}.", id);
            return StatusCode(500, ApiResponse.Error($"Failed to delete season: {ex.Message}"));
        }
    }
}
