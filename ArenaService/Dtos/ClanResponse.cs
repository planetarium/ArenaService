using ArenaService.Constants;

namespace ArenaService.Dtos;

public class ClanResponse
{
    public required string ImageURL { get; set; }
    public required string Name { get; set; }
    public int Rank { get; set; }
    public int Score { get; set; }
}
