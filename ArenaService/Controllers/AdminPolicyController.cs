namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("admin/policies")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminPolicyController : ControllerBase
{
    private readonly IBattleTicketPolicyRepository _battlePolicyRepo;
    private readonly IRefreshTicketPolicyRepository _refreshPolicyRepo;

    public AdminPolicyController(
        IBattleTicketPolicyRepository battlePolicyRepo,
        IRefreshTicketPolicyRepository refreshPolicyRepo
    )
    {
        _battlePolicyRepo = battlePolicyRepo;
        _refreshPolicyRepo = refreshPolicyRepo;
    }

    [HttpGet("battle-ticket")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBattleTicketPolicies()
    {
        var policies = await _battlePolicyRepo.GetAllBattlePoliciesAsync();
        return Ok(policies);
    }

    [HttpGet("battle-ticket/{id}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBattleTicketPolicy(int id)
    {
        var policy = await _battlePolicyRepo.GetBattlePolicyByIdAsync(id);
        if (policy == null)
        {
            return NotFound();
        }

        return Ok(policy);
    }

    [HttpPost("battle-ticket")]
    [SwaggerResponse(StatusCodes.Status201Created)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBattleTicketPolicy([FromBody] CreateBattleTicketPolicyRequest request)
    {
        if (request.PurchasePrices.Count != request.MaxPurchasableTicketsPerSeason)
        {
            return BadRequest($"PurchasePrices count ({request.PurchasePrices.Count}) must match MaxPurchasableTicketsPerSeason ({request.MaxPurchasableTicketsPerSeason}).");
        }

        var policy = new BattleTicketPolicy
        {
            Name = request.Name,
            DefaultTicketsPerRound = request.DefaultTicketsPerRound,
            MaxPurchasableTicketsPerRound = request.MaxPurchasableTicketsPerRound,
            MaxPurchasableTicketsPerSeason = request.MaxPurchasableTicketsPerSeason,
            PurchasePrices = request.PurchasePrices
        };

        var created = await _battlePolicyRepo.AddBattlePolicyAsync(policy);
        return CreatedAtAction(nameof(GetBattleTicketPolicy), new { id = created.Id }, created);
    }

    [HttpGet("refresh-ticket")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRefreshTicketPolicies()
    {
        var policies = await _refreshPolicyRepo.GetAllRefreshPoliciesAsync();
        return Ok(policies);
    }

    [HttpGet("refresh-ticket/{id}")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRefreshTicketPolicy(int id)
    {
        var policy = await _refreshPolicyRepo.GetRefreshPolicyByIdAsync(id);
        if (policy == null)
        {
            return NotFound();
        }

        return Ok(policy);
    }

    [HttpPost("refresh-ticket")]
    [SwaggerResponse(StatusCodes.Status201Created)]
    [SwaggerResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRefreshTicketPolicy([FromBody] CreateRefreshTicketPolicyRequest request)
    {
        if (request.PurchasePrices.Count != request.MaxPurchasableTicketsPerRound)
        {
            return BadRequest($"PurchasePrices count ({request.PurchasePrices.Count}) must match MaxPurchasableTicketsPerRound ({request.MaxPurchasableTicketsPerRound}).");
        }

        var policy = new RefreshTicketPolicy
        {
            Name = request.Name,
            DefaultTicketsPerRound = request.DefaultTicketsPerRound,
            MaxPurchasableTicketsPerRound = request.MaxPurchasableTicketsPerRound,
            PurchasePrices = request.PurchasePrices
        };

        var created = await _refreshPolicyRepo.AddRefreshPolicyAsync(policy);
        return CreatedAtAction(nameof(GetRefreshTicketPolicy), new { id = created.Id }, created);
    }
}
