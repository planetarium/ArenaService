namespace ArenaService.Controllers;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Extensions;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly ITicketRepository _ticketRepo;
    private readonly IBattleRepository _battleRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IParticipateService _participateService;

    public BattleController(
        IBattleRepository battleRepo,
        ITicketRepository ticketRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipateService participateService,
        IBackgroundJobClient jobClient
    )
    {
        _battleRepo = battleRepo;
        _ticketRepo = ticketRepo;
        _jobClient = jobClient;
        _availableOpponentRepo = availableOpponentRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
    }

    [HttpPost("token")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status201Created,
        "BattleTokenResponse",
        typeof(BattleTokenResponse)
    )]
    [SwaggerResponse(StatusCodes.Status423Locked, "Status423Locked", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateBattleToken(Address opponentAvatarAddress)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();
        if (
            cachedRound.EndBlock - ArenaServiceConfig.USE_TICKET_BLOCK_THRESHOLD
            <= cachedBlockIndex
        )
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                new ErrorResponse("ROUND_ENDING", "Round is about to end")
            );
        }
        var inProgressBattles = await _battleRepo.GetInProgressBattles(
            avatarAddress,
            opponentAvatarAddress,
            cachedSeason.Id,
            cachedRound.Id
        );
        if (inProgressBattles.Count > 0)
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                new ErrorResponse("BATTLE_IN_PROGRESS", "Battle is already in progress")
            );
        }

        var participant = await _participateService.ParticipateAsync(
            cachedSeason.Id,
            cachedRound.Id,
            avatarAddress,
            (int)(cachedRound.EndBlock - cachedRound.StartBlock),
            query =>
                query
                    .Include(p => p.User)
                    .Include(p => p.Season)
                    .ThenInclude(s => s.BattleTicketPolicy)
        );

        var battleTicketStatusPerRound = await _ticketRepo.GetBattleTicketStatusPerRound(
            cachedRound.Id,
            avatarAddress
        );
        var battleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            cachedSeason.Id,
            avatarAddress
        );

        if (battleTicketStatusPerRound is null)
        {
            battleTicketStatusPerRound = await _ticketRepo.AddBattleTicketStatusPerRound(
                cachedSeason.Id,
                cachedRound.Id,
                avatarAddress,
                participant.Season.BattleTicketPolicyId,
                participant.Season.BattleTicketPolicy.DefaultTicketsPerRound,
                0,
                0
            );
        }
        if (battleTicketStatusPerSeason is null)
        {
            await _ticketRepo.AddBattleTicketStatusPerSeason(
                cachedSeason.Id,
                avatarAddress,
                participant.Season.BattleTicketPolicyId,
                0,
                0
            );
        }

        if (battleTicketStatusPerRound.RemainingCount <= 0)
        {
            return BadRequest(new ErrorResponse("NO_TICKETS", "No remaining battle tickets"));
        }

        var availableOpponent = await _availableOpponentRepo.GetAvailableOpponent(
            avatarAddress,
            cachedRound.Id,
            opponentAvatarAddress
        );
        if (availableOpponent is null)
        {
            return BadRequest(new ErrorResponse("INVALID_OPPONENT", $"{opponentAvatarAddress} is not available opponent"));
        }

        var battle = await _battleRepo.AddBattleAsync(
            avatarAddress,
            cachedSeason.Id,
            cachedRound.Id,
            availableOpponent.Id
        );

        var locationUri = Url.Action(nameof(GetBattle), new { battleId = battle.Id });

        return Created(
            locationUri,
            new BattleTokenResponse { Token = battle.Token, BattleId = battle.Id }
        );
    }

    [HttpPost("{battleId}/request")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Ok")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Status403Forbidden", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(ErrorResponse))]
    public async Task<IActionResult> RequestBattle(int battleId, [FromBody] BattleRequest request)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var battle = await _battleRepo.GetBattleAsync(battleId);

        if (battle is null)
        {
            return NotFound(new ErrorResponse("BATTLE_NOT_FOUND", $"Battle log with ID {battleId} not found."));
        }

        if (battle.AvatarAddress != avatarAddress)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ErrorResponse("UNAUTHORIZED_ACCESS", "You are not authorized to access this battle")
            );
        }

        await _battleRepo.UpdateBattle(
            battle,
            b =>
            {
                b.TxId = request.TxId;
            }
        );

        _jobClient.Enqueue<BattleProcessor>(processor => processor.ProcessAsync(battleId));

        return Ok();
    }

    [HttpGet("{battleId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "BattleResponse", typeof(BattleResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Status403Forbidden", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(ErrorResponse))]
    public async Task<IActionResult> GetBattle(int battleId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var battle = await _battleRepo.GetBattleAsync(
            battleId,
            q => q.Include(b => b.AvailableOpponent).Include(b => b.Participant)
        );

        if (battle is null)
        {
            return NotFound(new ErrorResponse("BATTLE_NOT_FOUND", $"Battle log with ID {battleId} not found."));
        }

        if (battle.AvatarAddress != avatarAddress)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ErrorResponse("UNAUTHORIZED_ACCESS", "You are not authorized to access this battle")
            );
        }

        return Ok(battle.ToResponse());
    }
}
