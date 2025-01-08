namespace ArenaService.Controllers;

using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Worker;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/opponent/{opponentId}/battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IBattleLogRepository _battleLogRepo;

    public BattleController(
        IAvailableOpponentRepository availableOpponentService,
        IParticipantRepository participantService,
        IBattleLogRepository battleLogRepo,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentService;
        _participantRepo = participantService;
        _battleLogRepo = battleLogRepo;
        _jobClient = jobClient;
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(NotFound<string>), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<string>>
    > CreateBattleToken(int seasonId, int opponentId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var participant = await _participantRepo.GetParticipantByAvatarAddressAsync(
            seasonId,
            avatarAddress
        );

        if (participant is null)
        {
            return TypedResults.NotFound("Not participant user.");
        }

        var opponents = await _availableOpponentRepo.GetAvailableOpponents(participant.Id);
        var battleLog = await _battleLogRepo.AddBattleLogAsync(
            participant.Id,
            opponentId,
            seasonId,
            "token"
        );

        return TypedResults.Ok(battleLog.Token);
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(NotFound<string>), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<string>>
    > GetBattleResult(string txId, int logId)
    {
        _jobClient.Enqueue<BattleTaskProcessor>(processor => processor.ProcessAsync(txId));

        return TypedResults.Ok("test");
    }
}
