namespace ArenaService.Extensions;

using ArenaService.Dtos;
using ArenaService.Models;

public static class ParticipantExtensions
{
    public static ParticipantResponse ToResponse(this Participant participant)
    {
        return new ParticipantResponse
        {
            AvatarAddress = participant.AvatarAddress,
            NameWithHash = participant.NameWithHash,
            PortraitId = participant.PortraitId,
        };
    }
}
