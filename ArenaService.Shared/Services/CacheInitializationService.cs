using ArenaService.Shared.Repositories;

namespace ArenaService.Shared.Services;

public interface ICacheInitializationService
{
    Task<bool> InitializeCacheAsync(int seasonId, int roundIndex);
    Task<bool> InitializeAllCacheAsync();
    Task<bool> InitializeSeasonAndRoundCacheAsync();
}

public class CacheInitializationService : ICacheInitializationService
{
    private readonly IRankingRepository _rankingRepository;
    private readonly IRankingSnapshotRepository _rankingSnapshotRepository;

    public CacheInitializationService(
        IRankingRepository rankingRepository,
        IRankingSnapshotRepository rankingSnapshotRepository)
    {
        _rankingRepository = rankingRepository;
        _rankingSnapshotRepository = rankingSnapshotRepository;
    }

    public async Task<bool> InitializeCacheAsync(int seasonId, int roundIndex)
    {
        var rankingCount = await _rankingRepository.GetRankingCountAsync(seasonId, roundIndex);
        var snapshotCount = await _rankingSnapshotRepository.GetRankingSnapshotsCount(seasonId, roundIndex);
        
        if (rankingCount == 0 || snapshotCount < rankingCount)
        {
            return false;
        }

        await _rankingRepository.ClearRankingCacheAsync(seasonId, roundIndex);
        return true;
    }

    public async Task<bool> InitializeAllCacheAsync()
    {
        await _rankingRepository.ClearAllRankingCacheAsync();
        return true;
    }

    public async Task<bool> InitializeSeasonAndRoundCacheAsync()
    {
        await _rankingRepository.ClearSeasonAndRoundCacheAsync();
        return true;
    }
} 