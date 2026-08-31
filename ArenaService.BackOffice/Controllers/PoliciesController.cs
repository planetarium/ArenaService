using ArenaService.BackOffice.Authentication;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaService.BackOffice.Controllers;

/// <summary>
/// Battle / refresh ticket policies. Mirrors the Policy Blazor page.
/// Policies are append-only: existing rows are never mutated.
/// </summary>
[ApiController]
[Route("api/policies")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class PoliciesController : ControllerBase
{
    private readonly IBattleTicketPolicyRepository _battlePolicyRepository;
    private readonly IRefreshTicketPolicyRepository _refreshPolicyRepository;
    private readonly ILogger<PoliciesController> _logger;

    public PoliciesController(
        IBattleTicketPolicyRepository battlePolicyRepository,
        IRefreshTicketPolicyRepository refreshPolicyRepository,
        ILogger<PoliciesController> logger
    )
    {
        _battlePolicyRepository = battlePolicyRepository;
        _refreshPolicyRepository = refreshPolicyRepository;
        _logger = logger;
    }

    /// <summary>Lists every battle ticket policy.</summary>
    [HttpGet("battle")]
    public async Task<ActionResult<ApiResponse<List<BattleTicketPolicyDto>>>> GetBattlePolicies()
    {
        var policies = await _battlePolicyRepository.GetAllBattlePoliciesAsync();
        return Ok(
            ApiResponse<List<BattleTicketPolicyDto>>.Ok(
                policies.Select(BattleTicketPolicyDto.From).ToList()
            )
        );
    }

    /// <summary>Lists every refresh ticket policy.</summary>
    [HttpGet("refresh")]
    public async Task<ActionResult<ApiResponse<List<RefreshTicketPolicyDto>>>> GetRefreshPolicies()
    {
        var policies = await _refreshPolicyRepository.GetAllRefreshPoliciesAsync();
        return Ok(
            ApiResponse<List<RefreshTicketPolicyDto>>.Ok(
                policies.Select(RefreshTicketPolicyDto.From).ToList()
            )
        );
    }

    /// <summary>
    /// Adds a battle ticket policy. <c>purchasePrices</c> must have exactly
    /// <c>maxPurchasableTicketsPerSeason</c> entries, matching the UI validation.
    /// </summary>
    [HttpPost("battle")]
    public async Task<ActionResult<ApiResponse<BattleTicketPolicyDto>>> AddBattlePolicy(
        [FromBody] AddBattleTicketPolicyRequest request
    )
    {
        if (request.PurchasePrices.Count != request.MaxPurchasableTicketsPerSeason)
        {
            return BadRequest(
                ApiResponse<BattleTicketPolicyDto>.Error(
                    "Battle ticket prices must match the maximum season purchase count."
                )
            );
        }

        try
        {
            var policy = await _battlePolicyRepository.AddBattlePolicyAsync(
                new BattleTicketPolicy
                {
                    Name = request.Name,
                    DefaultTicketsPerRound = request.DefaultTicketsPerRound,
                    MaxPurchasableTicketsPerRound = request.MaxPurchasableTicketsPerRound,
                    MaxPurchasableTicketsPerSeason = request.MaxPurchasableTicketsPerSeason,
                    PurchasePrices = request.PurchasePrices
                }
            );
            return Ok(
                ApiResponse<BattleTicketPolicyDto>.Ok(
                    BattleTicketPolicyDto.From(policy),
                    "Battle ticket policy added."
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add battle ticket policy.");
            return StatusCode(
                500,
                ApiResponse<BattleTicketPolicyDto>.Error($"Failed to add battle ticket policy: {ex.Message}")
            );
        }
    }

    /// <summary>
    /// Adds a refresh ticket policy. <c>purchasePrices</c> must have exactly
    /// <c>maxPurchasableTicketsPerRound</c> entries, matching the UI validation.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<RefreshTicketPolicyDto>>> AddRefreshPolicy(
        [FromBody] AddRefreshTicketPolicyRequest request
    )
    {
        if (request.DefaultTicketsPerRound < 1)
        {
            return BadRequest(
                ApiResponse<RefreshTicketPolicyDto>.Error(
                    "Refresh tickets must have at least one default ticket."
                )
            );
        }

        if (request.PurchasePrices.Count != request.MaxPurchasableTicketsPerRound)
        {
            return BadRequest(
                ApiResponse<RefreshTicketPolicyDto>.Error(
                    "Refresh ticket prices must match the maximum round purchase count."
                )
            );
        }

        try
        {
            var policy = await _refreshPolicyRepository.AddRefreshPolicyAsync(
                new RefreshTicketPolicy
                {
                    Name = request.Name,
                    DefaultTicketsPerRound = request.DefaultTicketsPerRound,
                    MaxPurchasableTicketsPerRound = request.MaxPurchasableTicketsPerRound,
                    PurchasePrices = request.PurchasePrices
                }
            );
            return Ok(
                ApiResponse<RefreshTicketPolicyDto>.Ok(
                    RefreshTicketPolicyDto.From(policy),
                    "Refresh ticket policy added."
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add refresh ticket policy.");
            return StatusCode(
                500,
                ApiResponse<RefreshTicketPolicyDto>.Error(
                    $"Failed to add refresh ticket policy: {ex.Message}"
                )
            );
        }
    }
}
