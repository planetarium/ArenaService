using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArenaService.Shared.Services;

public interface ISeasonPreparationService
{
    Task PrepareSeasonAsync((Season Season, Round Round) seasonAndRound);
}

public class SeasonPreparationService : ISeasonPreparationService
{
    private readonly IParticipantRepository _participantRepo;
    private readonly IRankingSnapshotRepository _rankingSnapshotRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IClanRankingRepository _clanRankingRepo;
    private readonly IMedalRepository _medalRepo;
    private readonly ISeasonService _seasonService;
    private readonly IRankingService _rankingService;
    private readonly ILogger<SeasonPreparationService> _logger;

    private const int BatchSize = 1500;

    public SeasonPreparationService(
        IParticipantRepository participantRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IMedalRepository medalRepo,
        ISeasonService seasonService,
        IRankingService rankingService,
        ILogger<SeasonPreparationService> logger
    )
    {
        _participantRepo = participantRepo;
        _rankingSnapshotRepo = rankingSnapshotRepo;
        _rankingRepo = rankingRepo;
        _clanRankingRepo = clanRankingRepo;
        _medalRepo = medalRepo;
        _seasonService = seasonService;
        _rankingService = rankingService;
        _logger = logger;
    }

    public async Task PrepareSeasonAsync((Season Season, Round Round) seasonAndRound)
    {
        var prevSeasonId = seasonAndRound.Season.Id - 1;
        Dictionary<Address, int>? medalCounts = null;
        int skip = 0;

        while (true)
        {
            var prevSeasonParticipants = await _participantRepo.GetParticipantsAsync(
                prevSeasonId,
                skip,
                BatchSize,
                q => q.Where(p => p.Score > 1001).Include(p => p.User)
            );

            if (!prevSeasonParticipants.Any())
                break;

            _logger.LogInformation($"Init participants and ranking {prevSeasonParticipants.Count}");

            List<Participant> eligibleParticipants = await GetEligibleParticipantsAsync(
                seasonAndRound.Season,
                prevSeasonParticipants,
                medalCounts
            );

            await _participantRepo.AddParticipantsAsync(
                eligibleParticipants.Select(p => p.User).ToList(),
                seasonAndRound.Season.Id
            );

            List<(Address AvatarAddress, int? ClanId, int Score)> rankingData = eligibleParticipants
                .Select(p => (p.AvatarAddress, p.User.ClanId, 1000))
                .ToList();
            var clanRankingData = CreateClanRankingData(eligibleParticipants);

            await _rankingSnapshotRepo.AddRankingsSnapshot(
                rankingData,
                seasonAndRound.Season.Id,
                seasonAndRound.Round.Id
            );

            await InitializeRankings(
                rankingData.Select(r => (r.AvatarAddress, r.Score)).ToList(),
                clanRankingData,
                seasonAndRound.Season,
                seasonAndRound.Round
            );

            _logger.LogInformation($"Finish Init {eligibleParticipants.Count}");

            skip += BatchSize;
        }

        _logger.LogInformation($"PrepareNextSeason {seasonAndRound.Season.Id} Done");
    }

    private async Task<List<Participant>> GetEligibleParticipantsAsync(
        Season season,
        List<Participant> prevSeasonParticipants,
        Dictionary<Address, int>? medalCounts
    )
    {
        if (season.ArenaType != ArenaType.CHAMPIONSHIP)
            return prevSeasonParticipants.ToList();

        if (season.RequiredMedalCount <= 0)
            return prevSeasonParticipants.ToList();

        if (medalCounts is null)
        {
            var seasons = await _seasonService.ClassifyByChampionship(season.StartBlock + 1);
            var onlySeasons = seasons.Where(s => s.ArenaType == ArenaType.SEASON).ToList();

            if (!onlySeasons.Any())
                throw new NotFoundSeasonException("Not found seasons for check medals");

            medalCounts = await _medalRepo.GetMedalsBySeasonsAsync(
                onlySeasons.Select(s => s.Id).ToList()
            );
        }

        return prevSeasonParticipants
            .Where(p =>
                medalCounts.TryGetValue(p.AvatarAddress, out var totalMedals)
                && totalMedals >= season.RequiredMedalCount
            )
            .ToList();
    }

    private Dictionary<int, List<(Address, int)>> CreateClanRankingData(
        List<Participant> participants
    )
    {
        var clanRankingData = new Dictionary<int, List<(Address, int)>>();

        foreach (var participant in participants)
        {
            if (participant.User.ClanId is not null)
            {
                if (!clanRankingData.ContainsKey(participant.User.ClanId.Value))
                {
                    clanRankingData[participant.User.ClanId.Value] = new List<(Address, int)>();
                }

                clanRankingData[participant.User.ClanId.Value]
                    .Add((participant.AvatarAddress, 1000));
            }
        }

        return clanRankingData;
    }

    private async Task InitializeRankings(
        List<(Address AvatarAddress, int Score)> rankingData,
        Dictionary<int, List<(Address, int)>> clanRankingsData,
        Season season,
        Round round
    )
    {
        await _rankingRepo.InitRankingAsync(rankingData, season.Id, round.Id, season.RoundInterval);
        await _rankingRepo.InitRankingAsync(
            rankingData,
            season.Id,
            round.RoundIndex + 1,
            season.RoundInterval
        );

        foreach (var (clanId, clanRankingData) in clanRankingsData)
        {
            await _clanRankingRepo.InitRankingAsync(
                clanRankingData,
                clanId,
                season.Id,
                round.Id,
                season.RoundInterval
            );
            await _clanRankingRepo.InitRankingAsync(
                clanRankingData,
                clanId,
                season.Id,
                round.RoundIndex + 1,
                season.RoundInterval
            );
        }
        await _rankingService.UpdateAllClanRankingAsync(season.Id, round.RoundIndex, season.RoundInterval);
        await _rankingService.UpdateAllClanRankingAsync(
            season.Id,
            round.RoundIndex + 1,
            season.RoundInterval
        );
    }
}
