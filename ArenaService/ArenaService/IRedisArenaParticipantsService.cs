using Libplanet.Crypto;

namespace ArenaService;

public interface IRedisArenaParticipantsService
{
    Task<List<ArenaParticipant>> GetArenaParticipantsAsync(string key);
    Task SetArenaParticipantsAsync(string key, List<ArenaParticipant> value, TimeSpan? expiry = null);
    Task<string> GetSeasonKeyAsync();
    Task SetSeasonAsync(string value, TimeSpan? expiry = null);
    Task<List<(Address avatarAddr, int score, int rank)>> GetAvatarAddrAndScoresWithRank(string key);
    Task SetAvatarAddrAndScoresWithRank(string key, List<(Address avatarAddr, int score, int rank)> value,
        TimeSpan? expiry = null);
}
