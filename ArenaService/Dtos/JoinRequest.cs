namespace ArenaService.Dtos;

public class JoinRequest
{
    public required string AvatarAddress { get; set; }
    public required string NameWithHash { get; set; }
    public required int PortraitId { get; set; }
    public required string AuthToken { get; set; }
}
