using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArenaService.Shared.Services;

public interface IRoundPreparationService
{
    Task PrepareNextRoundWithSnapshotAsync((Season Season, Round Round) seasonAndRound);
}

public class RoundPreparationService : IRoundPreparationService
{
    private readonly IRankingSnapshotRepository _rankingSnapshotRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IClanRankingRepository _clanRankingRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IClanRepository _clanRepo;
    private readonly IRankingService _rankingService;
    private readonly ILogger<RoundPreparationService> _logger;

    public RoundPreparationService(
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IClanRepository clanRepo,
        IParticipantRepository participantRepo,
        IRankingService rankingService,
        ILogger<RoundPreparationService> logger
    )
    {
        _rankingSnapshotRepo = rankingSnapshotRepo;
        _rankingRepo = rankingRepo;
        _clanRankingRepo = clanRankingRepo;
        _participantRepo = participantRepo;
        _clanRepo = clanRepo;
        _rankingService = rankingService;
        _logger = logger;
    }

    public async Task PrepareNextRoundWithSnapshotAsync((Season Season, Round Round) seasonAndRound)
    {
        _logger.LogInformation($"Start PrepareNextRound {seasonAndRound.Round.Id}");

        var firstRound = seasonAndRound.Season.Rounds.OrderBy(r => r.StartBlock).First();

        if (firstRound.Id == seasonAndRound.Round.Id)
        {
            _logger.LogInformation($"First round, skip");
            return;
        }

        var clanIds = await _clanRankingRepo.GetClansAsync(
            seasonAndRound.Season.Id,
            seasonAndRound.Round.Id
        );

        var participants = new List<Participant>();
        int skip = 0;
        while (true)
        {
            var newParticipants = await _participantRepo.GetParticipantsAsync(
                seasonAndRound.Season.Id,
                skip,
                300
            );

            if (!newParticipants.Any())
                break;

            participants.AddRange(newParticipants);
            skip += 300;
        }
        var rankingData = participants.Select(p => (p.AvatarAddress, p.Score)).ToList();

        await _rankingRepo.InitRankingAsync(
            rankingData,
            seasonAndRound.Season.Id,
            seasonAndRound.Round.Id + 1,
            seasonAndRound.Season.RoundInterval
        );

        var clans = await GetClanMappingsAsync(clanIds, seasonAndRound);

        var rankingDataWithClan = rankingData
            .Select(entry =>
                (
                    entry.AvatarAddress,
                    clans.TryGetValue(entry.AvatarAddress, out var value) ? value : (int?)null,
                    entry.Score
                )
            )
            .ToList();

        await _rankingSnapshotRepo.AddRankingsSnapshot(
            rankingDataWithClan,
            seasonAndRound.Season.Id,
            seasonAndRound.Round.Id
        );

        await _rankingService.UpdateAllClanRankingAsync(
            seasonAndRound.Season.Id,
            seasonAndRound.Round.Id + 1,
            seasonAndRound.Season.RoundInterval
        );

        _logger.LogInformation($"PrepareNextRound {seasonAndRound.Round.Id} Done");
    }

    private async Task<Dictionary<Address, int>> GetClanMappingsAsync(
        IEnumerable<int> clanIds,
        (Season Season, Round Round) seasonAndRound
    )
    {
        var clans = new Dictionary<Address, int>();

        foreach (var clanId in clanIds)
        {
            await _clanRankingRepo.CopyRoundDataAsync(
                clanId,
                seasonAndRound.Season.Id,
                seasonAndRound.Round.Id,
                seasonAndRound.Round.Id + 1,
                seasonAndRound.Season.RoundInterval
            );

            var clan = await _clanRepo.GetClan(clanId, q => q.Include(c => c.Users));
            if (clan != null)
            {
                foreach (var user in clan.Users)
                {
                    clans[user.AvatarAddress] = clanId;
                }
            }
        }

        return clans;
    }
}
