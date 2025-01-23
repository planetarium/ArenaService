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

    public ParticipateService(
        IParticipantRepository participantRepo,
        IUserRepository userRepo,
        IRankingRepository rankingRepo
    )
    {
        _participantRepo = participantRepo;
        _userRepo = userRepo;
        _rankingRepo = rankingRepo;
    }

    public async Task<Participant> ParticipateAsync(
        int seasonId,
        int roundId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    )
    {
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

        return participant;
    }
}
