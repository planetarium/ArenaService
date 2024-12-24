namespace ArenaService.Dtos;

public class ParticipantResponse
{
    public required string AvatarAddress { get; set; }

    public required string NameWithHash { get; set; }

    public int PortraitId { get; set; }
}
