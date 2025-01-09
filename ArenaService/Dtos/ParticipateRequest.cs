namespace ArenaService.Dtos;

public class ParticipateRequest
{
    public required string NameWithHash { get; set; }
    public required int PortraitId { get; set; }
    public long Cp { get; set; }
    public int Level { get; set; }
}
