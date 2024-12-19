using Libplanet.Crypto;

namespace ArenaService;

public interface IRedisArenaParticipantsService
{
    Task<List<ArenaParticipantStruct>> GetArenaParticipantsAsync(string key);
    Task SetArenaParticipantsAsync(string key, List<ArenaParticipantStruct> value, TimeSpan? expiry = null);
    Task<string> GetSeasonKeyAsync();
    Task SetSeasonAsync(string value, TimeSpan? expiry = null);
    Task<List<AvatarAddressAndScore>> GetAvatarAddrAndScores(string key);
    Task SetAvatarAddrAndScores(string key, List<AvatarAddressAndScore> value, TimeSpan? expiry = null);
}
