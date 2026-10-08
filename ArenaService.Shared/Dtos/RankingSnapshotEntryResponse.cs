using Libplanet.Crypto;

namespace ArenaService.Shared.Dtos;

public class RankingSnapshotEntryResponse
{
    public required Address AgentAddress { get; set; }
    public required Address AvatarAddress { get; set; }
    public required string NameWithHash { get; set; }
    public required int Level { get; set; }
    public required long Cp { get; set; }
    public required int Score { get; set; }
}


