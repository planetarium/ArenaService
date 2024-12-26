namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Exceptions;
using ArenaService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/participants")]
[ApiController]
public class ParticipantController : ControllerBase
{
    private readonly ParticipantService _participantService;
    private readonly SeasonService _seasonService;

    public ParticipantController(ParticipantService participantService, SeasonService seasonService)
    {
        _participantService = participantService;
        _seasonService = seasonService;
    }

    [HttpPost]
    public async Task<Results<NotFound<string>, Created>> Join(
        int seasonId,
        [FromBody] JoinRequest joinRequest
    )
    {
        if (await _seasonService.IsActivatedSeason(seasonId))
        {
            await _participantService.AddParticipantAsync(seasonId, joinRequest);
            return TypedResults.Created();
        }

        return TypedResults.NotFound("No active season found.");
    }
}
