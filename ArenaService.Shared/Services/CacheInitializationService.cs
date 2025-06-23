using ArenaService.Shared.Repositories;

namespace ArenaService.Shared.Services;

public interface ICacheInitializationService
{
    Task<bool> InitializeRankingCacheAsync(int seasonId, int roundIndex);
}

public class CacheInitializationService : ICacheInitializationService
{
    private readonly IRankingRepository _rankingRepository;
    private readonly IRankingSnapshotRepository _rankingSnapshotRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly ISeasonCacheRepository _seasonCacheRepository;

    public CacheInitializationService(
        IRankingRepository rankingRepository,
        IRankingSnapshotRepository rankingSnapshotRepository,
        IParticipantRepository participantRepository,
        ISeasonCacheRepository seasonCacheRepository
    )
    {
        _rankingRepository = rankingRepository;
        _rankingSnapshotRepository = rankingSnapshotRepository;
        _participantRepository = participantRepository;
        _seasonCacheRepository = seasonCacheRepository;
    }

    public async Task<bool> InitializeRankingCacheAsync(int seasonId, int roundIndex)
    {
        var snapshotCount = await _rankingSnapshotRepository.GetRankingSnapshotsCount(
            seasonId,
            roundIndex
        );
        var participantCount = await _participantRepository.GetParticipantCountAsync(seasonId);

        if (snapshotCount < participantCount - 50)
        {
            return false;
        }

        await _rankingRepository.ClearAllRankingCacheAsync();
        await _seasonCacheRepository.DeleteAllAsync();
        return true;
    }
}
