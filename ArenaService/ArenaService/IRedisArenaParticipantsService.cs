using ArenaService.Models;

namespace ArenaService;

public interface IRedisArenaParticipantsService
{
    Task<List<ArenaParticipant>> GetArenaParticipantsAsync(string key);
    Task SetArenaParticipantsAsync(string key, List<ArenaParticipant> value, TimeSpan? expiry = null);
    Task<string> GetSeasonKeyAsync();
    Task SetSeasonAsync(string value, TimeSpan? expiry = null);
    Task<List<AvatarAddressAndScore>> GetAvatarAddrAndScores(string key);
    Task SetAvatarAddrAndScores(string key, List<AvatarAddressAndScore> value, TimeSpan? expiry = null);
}
