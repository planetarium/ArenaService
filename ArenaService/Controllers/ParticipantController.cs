namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Exceptions;
using ArenaService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("api/seasons/{seasonId}/participants")]
[ApiController]
public class ParticipantController : ControllerBase
{
    private readonly ParticipantService _participantService;

    public ParticipantController(ParticipantService participantService)
    {
        _participantService = participantService;
    }

    [HttpPost]
    public async Task<Results<NotFound<string>, Created>> Join(
        int seasonId,
        [FromBody] JoinRequest joinRequest
    )
    {
        try
        {
            var participantResponse = await _participantService.AddParticipantAsync(
                seasonId,
                joinRequest
            );
            return TypedResults.Created();
        }
        catch (SeasonNotFoundException)
        {
            return TypedResults.NotFound("No active season found.");
        }
    }
}
