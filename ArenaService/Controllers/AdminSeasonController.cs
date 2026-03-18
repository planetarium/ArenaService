namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("admin/seasons")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminSeasonController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepo;
    private readonly ISeasonService _seasonService;
    private readonly ISeasonBlockAdjustmentService _adjustmentService;
    private readonly ISeasonCacheRepository _seasonCacheRepo;

    public AdminSeasonController(
        ISeasonRepository seasonRepo,
        ISeasonService seasonService,
        ISeasonBlockAdjustmentService adjustmentService,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        _seasonRepo = seasonRepo;
        _seasonService = seasonService;
        _adjustmentService = adjustmentService;
        _seasonCacheRepo = seasonCacheRepo;
    }

    [HttpPost]
    [SwaggerResponse(StatusCodes.Status201Created, "Season created")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Block range overlapping")]
    public async Task<IActionResult> CreateSeason([FromBody] CreateSeasonRequest request)
    {
        try
        {
            var season = await _seasonRepo.AddSeasonWithRoundsAsync(
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

            return CreatedAtAction(nameof(GetSeason), new { seasonId = season.Id }, season);
        }
        catch (InvalidOperationException)
        {
            return Conflict("Block range overlaps with existing season.");
        }
    }

    [HttpGet("{seasonId}")]
    [SwaggerResponse(StatusCodes.Status200OK, "Season details")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeason(int seasonId)
    {
        try
        {
            var season = await _seasonRepo.GetSeasonAsync(
                seasonId,
                q => q.Include(s => s.BattleTicketPolicy)
                      .Include(s => s.RefreshTicketPolicy)
                      .Include(s => s.Rounds)
            );
            return Ok(season);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPut("{seasonId}")]
    [SwaggerResponse(StatusCodes.Status200OK, "Season updated")]
    public async Task<IActionResult> UpdateSeason(int seasonId, [FromBody] UpdateSeasonRequest request)
    {
        try
        {
            var season = await _seasonRepo.UpdateSeasonAsync(
                seasonId,
                request.SeasonGroupId,
                request.ArenaType,
                request.RoundInterval,
                request.RequiredMedalCount,
                request.TotalPrize,
                request.BattleTicketPolicyId,
                request.RefreshTicketPolicyId
            );

            return Ok(season);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{seasonId}")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Season deleted")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Season already started")]
    public async Task<IActionResult> DeleteSeason(int seasonId)
    {
        var currentBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var canDelete = await _seasonService.CanDeleteSeasonAsync(seasonId, currentBlockIndex);
        if (!canDelete)
        {
            return BadRequest("Cannot delete a season that has already started.");
        }

        await _seasonService.DeleteSeasonAsync(seasonId);
        return NoContent();
    }

    [HttpPut("{seasonId}/end-block")]
    [SwaggerResponse(StatusCodes.Status200OK, "End block adjusted")]
    public async Task<IActionResult> AdjustEndBlock(int seasonId, [FromBody] AdjustEndBlockRequest request)
    {
        try
        {
            var season = await _adjustmentService.AdjustSeasonEndBlockAsync(seasonId, request.NewEndBlock);
            return Ok(season);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}
