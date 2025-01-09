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
            NameWithHash = participant.User.NameWithHash,
            PortraitId = participant.User.PortraitId,
            Cp = participant.User.Cp,
            Level = participant.User.Level,
            Score = participant.Score,
            SeasonId = participant.SeasonId,
        };
    }

    public static List<ParticipantResponse> ToResponse(this List<Participant> participants)
    {
        return participants.Select(p => p.ToResponse()).ToList();
    }
}
