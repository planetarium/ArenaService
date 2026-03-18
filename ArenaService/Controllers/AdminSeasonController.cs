namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Extensions;
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
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid parameters")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Block range overlapping")]
    public async Task<IActionResult> CreateSeason([FromBody] CreateSeasonRequest request)
    {
        try
        {
            checked
            {
                _ = request.StartBlock + ((long)request.RoundInterval * request.RoundCount) - 1;
            }
        }
        catch (OverflowException)
        {
            return BadRequest("RoundInterval * RoundCount causes overflow.");
        }

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

            var created = await _seasonRepo.GetSeasonAsync(
                season.Id,
                q => q.Include(s => s.BattleTicketPolicy)
                      .Include(s => s.RefreshTicketPolicy)
                      .Include(s => s.Rounds)
            );

            return CreatedAtAction(nameof(GetSeason), new { seasonId = created.Id }, created.ToResponse());
        }
        catch (InvalidOperationException)
        {
            return Conflict("Block range overlaps with existing season.");
        }
    }

    [HttpGet("{seasonId}")]
    [SwaggerResponse(StatusCodes.Status200OK, "Season details", typeof(SeasonResponse))]
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
            return Ok(season.ToResponse());
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPut("{seasonId}")]
    [SwaggerResponse(StatusCodes.Status200OK, "Season updated", typeof(SeasonResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeason(int seasonId, [FromBody] UpdateSeasonRequest request)
    {
        try
        {
            await _seasonRepo.UpdateSeasonAsync(
                seasonId,
                request.SeasonGroupId,
                request.ArenaType,
                request.RoundInterval,
                request.RequiredMedalCount,
                request.TotalPrize,
                request.BattleTicketPolicyId,
                request.RefreshTicketPolicyId
            );

            var updated = await _seasonRepo.GetSeasonAsync(
                seasonId,
                q => q.Include(s => s.BattleTicketPolicy)
                      .Include(s => s.RefreshTicketPolicy)
                      .Include(s => s.Rounds)
            );

            return Ok(updated.ToResponse());
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{seasonId}")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Season deleted")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Season already started")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSeason(int seasonId)
    {
        try
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
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPut("{seasonId}/end-block")]
    [SwaggerResponse(StatusCodes.Status200OK, "End block adjusted", typeof(SeasonResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid end block")]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustEndBlock(int seasonId, [FromBody] AdjustEndBlockRequest request)
    {
        try
        {
            var season = await _seasonRepo.GetSeasonAsync(seasonId);

            if (request.NewEndBlock < season.StartBlock)
            {
                return BadRequest($"NewEndBlock ({request.NewEndBlock}) must be >= season start block ({season.StartBlock}).");
            }

            await _adjustmentService.AdjustSeasonEndBlockAsync(seasonId, request.NewEndBlock);

            var adjusted = await _seasonRepo.GetSeasonAsync(
                seasonId,
                q => q.Include(s => s.BattleTicketPolicy)
                      .Include(s => s.RefreshTicketPolicy)
                      .Include(s => s.Rounds)
            );

            return Ok(adjusted.ToResponse());
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}
