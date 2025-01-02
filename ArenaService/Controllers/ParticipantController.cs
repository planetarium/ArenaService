namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/participants")]
[ApiController]
public class ParticipantController : ControllerBase
{
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonRepository _seasonRepo;

    public ParticipantController(
        IParticipantRepository participantRepo,
        ISeasonRepository seasonRepo
    )
    {
        _participantRepo = participantRepo;
        _seasonRepo = seasonRepo;
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    public async Task<Results<UnauthorizedHttpResult, NotFound<string>, Created>> Join(
        int seasonId,
        [FromBody] JoinRequest joinRequest
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var season = await _seasonRepo.GetSeasonAsync(seasonId);

        if (season is not null && season.IsActivated)
        {
            await _participantRepo.InsertParticipantToSpecificSeasonAsync(
                seasonId,
                avatarAddress,
                joinRequest.NameWithHash,
                joinRequest.PortraitId
            );
            return TypedResults.Created();
        }

        return TypedResults.NotFound("No active season found.");
    }
}
