namespace ArenaService.Controllers;

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

    [HttpPost("battle-ticket")]
    [SwaggerResponse(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBattleTicketPolicy([FromBody] CreateBattleTicketPolicyRequest request)
    {
        var policy = new BattleTicketPolicy
        {
            Name = request.Name,
            DefaultTicketsPerRound = request.DefaultTicketsPerRound,
            MaxPurchasableTicketsPerRound = request.MaxPurchasableTicketsPerRound,
            MaxPurchasableTicketsPerSeason = request.MaxPurchasableTicketsPerSeason,
            PurchasePrices = request.PurchasePrices
        };

        var created = await _battlePolicyRepo.AddBattlePolicyAsync(policy);
        return CreatedAtAction(nameof(GetBattleTicketPolicies), new { id = created.Id }, created);
    }

    [HttpGet("refresh-ticket")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRefreshTicketPolicies()
    {
        var policies = await _refreshPolicyRepo.GetAllRefreshPoliciesAsync();
        return Ok(policies);
    }

    [HttpPost("refresh-ticket")]
    [SwaggerResponse(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRefreshTicketPolicy([FromBody] CreateRefreshTicketPolicyRequest request)
    {
        var policy = new RefreshTicketPolicy
        {
            Name = request.Name,
            DefaultTicketsPerRound = request.DefaultTicketsPerRound,
            MaxPurchasableTicketsPerRound = request.MaxPurchasableTicketsPerRound,
            PurchasePrices = request.PurchasePrices
        };

        var created = await _refreshPolicyRepo.AddRefreshPolicyAsync(policy);
        return CreatedAtAction(nameof(GetRefreshTicketPolicies), new { id = created.Id }, created);
    }
}

public class CreateBattleTicketPolicyRequest
{
    public required string Name { get; set; }
    public int DefaultTicketsPerRound { get; set; }
    public int MaxPurchasableTicketsPerRound { get; set; }
    public int MaxPurchasableTicketsPerSeason { get; set; }
    public List<decimal> PurchasePrices { get; set; } = new();
}

public class CreateRefreshTicketPolicyRequest
{
    public required string Name { get; set; }
    public int DefaultTicketsPerRound { get; set; }
    public int MaxPurchasableTicketsPerRound { get; set; }
    public List<decimal> PurchasePrices { get; set; } = new();
}
