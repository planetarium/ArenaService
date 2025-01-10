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
    private readonly IUserRepository _userRepo;
    private readonly IRankingRepository _rankingRepo;

    public ParticipantController(
        IParticipantRepository participantRepo,
        ISeasonRepository seasonRepo,
        IUserRepository userRepo,
        IRankingRepository rankingRepo
    )
    {
        _participantRepo = participantRepo;
        _seasonRepo = seasonRepo;
        _userRepo = userRepo;
        _rankingRepo = rankingRepo;
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(SeasonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<Results<NotFound<string>, Conflict<string>, Created>> Participate(
        int seasonId,
        [FromBody] ParticipateRequest participateRequest
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();
        var agentAddress = HttpContext.User.RequireAgentAddress();

        var season = await _seasonRepo.GetSeasonAsync(seasonId);

        if (season is null)
        {
            return TypedResults.NotFound("No season found.");
        }

        var existingParticipant = await _participantRepo.GetParticipantAsync(
            seasonId,
            avatarAddress
        );
        if (existingParticipant is not null)
        {
            return TypedResults.Conflict(
                $"User with AvatarAddress {avatarAddress} is already participating in this season."
            );
        }

        await _userRepo.AddOrGetUserAsync(
            agentAddress,
            avatarAddress,
            participateRequest.NameWithHash,
            participateRequest.PortraitId,
            participateRequest.Cp,
            participateRequest.Level
        );
        var participant = await _participantRepo.AddParticipantAsync(seasonId, avatarAddress);

        var rankingKey = $"ranking:season:{seasonId}";

        await _rankingRepo.UpdateScoreAsync(
            rankingKey,
            participant.AvatarAddress,
            seasonId,
            participant.Score
        );
        return TypedResults.Created();
    }
}
