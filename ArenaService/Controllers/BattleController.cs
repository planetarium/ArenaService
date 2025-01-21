namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IBattleRepository _battleRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly ParticipateService _participateService;

    public BattleController(
        IBattleRepository battleRepo,
        ISeasonCacheRepository seasonCacheRepo,
        ParticipateService participateService,
        IBackgroundJobClient jobClient
    )
    {
        _battleRepo = battleRepo;
        _jobClient = jobClient;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
    }

    [HttpGet("token")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(BattleTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<
        Results<
            UnauthorizedHttpResult,
            NotFound<string>,
            StatusCodeHttpResult,
            Ok<BattleTokenResponse>
        >
    > CreateBattleToken(string opponentAvatarAddress)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        await _participateService.ParticipateAsync(cachedSeason.Id, avatarAddress);

        var defenderAvatarAddress = new Address(opponentAvatarAddress);

        var battle = await _battleRepo.AddBattleAsync(
            cachedSeason.Id,
            avatarAddress,
            defenderAvatarAddress,
            "token"
        );

        return TypedResults.Ok(
            new BattleTokenResponse { Token = battle.Token, BattleId = battle.Id }
        );
    }

    [HttpPost("{battleId}/request")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<Results<UnauthorizedHttpResult, NotFound<string>, Ok>> RequestBattle(
        int battleId,
        [FromBody] BattleRequest request
    )
    {
        var battle = await _battleRepo.GetBattleAsync(battleId);

        if (battle is null)
        {
            return TypedResults.NotFound($"Battle log with ID {battleId} not found.");
        }

        _jobClient.Enqueue<BattleProcessor>(processor =>
            processor.ProcessAsync(TxId.FromHex(request.TxId), battleId)
        );

        return TypedResults.Ok();
    }

    [HttpGet("{battleId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(BattleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<BattleResponse>>
    > GetBattle(int battleId)
    {
        var battle = await _battleRepo.GetBattleAsync(battleId);

        if (battle is null)
        {
            return TypedResults.NotFound($"Battle log with ID {battleId} not found.");
        }

        return TypedResults.Ok(battle.ToResponse());
    }
}
