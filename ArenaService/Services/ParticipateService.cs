namespace ArenaService.Services;

using System.Threading.Tasks;
using ArenaService.Models;
using ArenaService.Repositories;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IParticipateService
{
    Task<Participant> ParticipateAsync(
        int seasonId,
        int roundId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    );
}

public class ParticipateService : IParticipateService
{
    private readonly IParticipantRepository _participantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IGroupRankingRepository _groupRankingRepo;
    private readonly IClanRankingRepository _clanRankingRepo;

    public ParticipateService(
        IParticipantRepository participantRepo,
        IUserRepository userRepo,
        IRankingRepository rankingRepo,
        IGroupRankingRepository groupRankingRepo,
        IClanRankingRepository clanRankingRepo
    )
    {
        _participantRepo = participantRepo;
        _userRepo = userRepo;
        _rankingRepo = rankingRepo;
        _groupRankingRepo = groupRankingRepo;
        _clanRankingRepo = clanRankingRepo;
    }

    public async Task<Participant> ParticipateAsync(
        int seasonId,
        int roundId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    )
    {
        includeQuery ??= q => q;
        includeQuery = q => includeQuery(q).Include(p => p.User);

        var existingParticipant = await _participantRepo.GetParticipantAsync(
            seasonId,
            avatarAddress,
            includeQuery
        );
        if (existingParticipant is not null)
        {
            return existingParticipant;
        }

        await _userRepo.GetUserAsync(avatarAddress);

        var participant = await _participantRepo.AddParticipantAsync(seasonId, avatarAddress);

        await _rankingRepo.UpdateScoreAsync(
            participant.AvatarAddress,
            seasonId,
            roundId,
            participant.Score
        );
        await _rankingRepo.UpdateScoreAsync(
            participant.AvatarAddress,
            seasonId,
            roundId + 1,
            participant.Score
        );

        await _groupRankingRepo.UpdateScoreAsync(
            participant.AvatarAddress,
            seasonId,
            roundId,
            0,
            participant.Score
        );
        await _groupRankingRepo.UpdateScoreAsync(
            participant.AvatarAddress,
            seasonId,
            roundId + 1,
            0,
            participant.Score
        );

        if (participant.User.ClanId is not null)
        {
            await _clanRankingRepo.UpdateScoreAsync(
                participant.User.ClanId.Value,
                seasonId,
                roundId,
                participant.Score
            );
            await _clanRankingRepo.UpdateScoreAsync(
                participant.User.ClanId.Value,
                seasonId,
                roundId + 1,
                participant.Score
            );
        }

        return participant;
    }
}
