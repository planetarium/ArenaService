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
    private readonly IBlockTrackerRepository _blockTrackerRepository;

    public CacheInitializationService(
        IRankingRepository rankingRepository,
        IRankingSnapshotRepository rankingSnapshotRepository,
        IParticipantRepository participantRepository,
        ISeasonCacheRepository seasonCacheRepository,
        IBlockTrackerRepository blockTrackerRepository
    )
    {
        _rankingRepository = rankingRepository;
        _rankingSnapshotRepository = rankingSnapshotRepository;
        _participantRepository = participantRepository;
        _seasonCacheRepository = seasonCacheRepository;
        _blockTrackerRepository = blockTrackerRepository;
    }

    public async Task<bool> InitializeRankingCacheAsync(int seasonId, int roundId)
    {
        var snapshotCount = await _rankingSnapshotRepository.GetRankingSnapshotsCount(
            seasonId,
            roundId
        );
        var participantCount = await _participantRepository.GetParticipantCountAsync(seasonId);

        if (snapshotCount < participantCount - 50)
        {
            return false;
        }

        await Task.WhenAll(
            _rankingRepository.ClearAllRankingCacheAsync(),
            _seasonCacheRepository.DeleteAllAsync(),
            _blockTrackerRepository.DeleteBattleTxTrackerBlockIndexAsync()
        );
        return true;
    }
}
