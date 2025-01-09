namespace ArenaService.Dtos;

public class ParticipantResponse
{
    public required string AvatarAddress { get; set; }

    public required string NameWithHash { get; set; }

    public int PortraitId { get; set; }
    public long Cp { get; set; }
    public int Level { get; set; }
    public int SeasonId { get; set; }
    public int Score { get; set; }
}
