namespace ArenaService;

public interface IRedisArenaParticipantsService
{
    Task<List<ArenaParticipant>> GetValueAsync(string key);
    Task SetValueAsync(string key, List<ArenaParticipant> value, TimeSpan? expiry = null);
}
