namespace ArenaService.Dtos;

public class JoinRequest
{
    public required string NameWithHash { get; set; }
    public required int PortraitId { get; set; }
}
