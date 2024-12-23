namespace ArenaService.Services;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Repositories;

public class ParticipantService
{
    private readonly IParticipantRepository _participantRepository;

    public ParticipantService(IParticipantRepository participantRepository)
    {
        _participantRepository = participantRepository;
    }

    public async Task<ParticipantResponse> AddParticipantAsync(
        int seasonId,
        JoinRequest joinRequest
    )
    {
        var participant = await _participantRepository.InsertParticipantToSpecificSeason(
            seasonId,
            joinRequest.AvatarAddress,
            joinRequest.NameWithHash,
            joinRequest.PortraitId
        );
        return participant.ToResponse();
    }
}
