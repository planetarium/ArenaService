namespace ArenaService.Shared.Services;

using System.Threading.Tasks;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IParticipateService
{
    Task<Participant> ParticipateAsync(
        int seasonId,
        int roundId,
        int roundIndex,
        Address avatarAddress,
        int roundInterval,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    );
}

public class ParticipateService : IParticipateService
{
    private readonly IParticipantRepository _participantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IRankingSnapshotRepository _rankingSnapshotRepo;
    private readonly ISeasonService _seasonService;
    private readonly ISeasonCacheRepository _seasonCache;
    private readonly IMedalRepository _medalRepo;
    private readonly IClanRankingRepository _clanRankingRepo;

    public ParticipateService(
        IParticipantRepository participantRepo,
        IUserRepository userRepo,
        IRankingRepository rankingRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        ISeasonService seasonService,
        IMedalRepository medalRepo,
        ISeasonCacheRepository seasonCache,
        IClanRankingRepository clanRankingRepo
    )
    {
        _participantRepo = participantRepo;
        _userRepo = userRepo;
        _rankingRepo = rankingRepo;
        _rankingSnapshotRepo = rankingSnapshotRepo;
        _seasonService = seasonService;
        _seasonCache = seasonCache;
        _medalRepo = medalRepo;
        _clanRankingRepo = clanRankingRepo;
    }

    public async Task<Participant> ParticipateAsync(
        int seasonId,
        int roundId,
        int roundIndex,
        Address avatarAddress,
        int roundInterval,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    )
    {
        includeQuery ??= q => q;
        var finalQuery = (IQueryable<Participant> q) => includeQuery(q).Include(p => p.User);

        var existingParticipant = await _participantRepo.GetParticipantAsync(
            seasonId,
            avatarAddress,
            finalQuery
        );
        if (existingParticipant is not null)
        {
            return existingParticipant;
        }

        var user = await _userRepo.GetUserAsync(avatarAddress);

        if (user is null)
        {
            throw new NotRegisteredUserException("Register first");
        }

        var currentSeason = await _seasonService.GetSeasonAndRoundByBlock(
            await _seasonCache.GetBlockIndexAsync()
        );
        if (currentSeason.Season.ArenaType == ArenaType.CHAMPIONSHIP)
        {
            if (currentSeason.Season.RequiredMedalCount > 0)
            {
                var totalMedalCount = 0;
                var seasons = await _seasonService.ClassifyByChampionship(
                    currentSeason.Season.StartBlock
                );
                var onlySeasons = seasons.Where(s => s.ArenaType == ArenaType.SEASON).ToList();
                if (!onlySeasons.Any())
                {
                    throw new NotFoundSeasonException("Not found seasons for check medals");
                }

                foreach (var season in onlySeasons)
                {
                    var medal = await _medalRepo.GetMedalAsync(season.Id, avatarAddress);
                    if (medal is not null)
                    {
                        totalMedalCount += medal.MedalCount;
                    }
                }

                if (totalMedalCount < currentSeason.Season.RequiredMedalCount)
                {
                    throw new NotEnoughMedalException($"{totalMedalCount} is not enough");
                }
            }
        }

        var participant = await _participantRepo.AddParticipantAsync(seasonId, avatarAddress);

        await _rankingSnapshotRepo.AddRankingsSnapshot(
            [(avatarAddress, user.ClanId, participant.Score)],
            seasonId,
            roundId
        );
        await _rankingRepo.UpdateScoreAsync(
            participant.AvatarAddress,
            seasonId,
            roundIndex,
            participant.Score
        );
        await _rankingRepo.UpdateScoreAsync(
            participant.AvatarAddress,
            seasonId,
            roundIndex + 1,
            participant.Score
        );

        if (user.ClanId is not null)
        {
            await _clanRankingRepo.UpdateScoreAsync(
                user.ClanId.Value,
                avatarAddress,
                seasonId,
                roundIndex,
                participant.Score
            );
            await _clanRankingRepo.UpdateScoreAsync(
                user.ClanId.Value,
                avatarAddress,
                seasonId,
                roundIndex + 1,
                participant.Score
            );
        }

        return participant;
    }
}
