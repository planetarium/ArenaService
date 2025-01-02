namespace ArenaService.Controllers;

using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/opponent/{opponentId}/battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IBattleLogRepository _battleLogRepo;

    public BattleController(
        IAvailableOpponentRepository availableOpponentService,
        IParticipantRepository participantService,
        IBattleLogRepository battleLogRepo
    )
    {
        _availableOpponentRepo = availableOpponentService;
        _participantRepo = participantService;
        _battleLogRepo = battleLogRepo;
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
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
}
