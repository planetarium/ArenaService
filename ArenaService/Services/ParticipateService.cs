namespace ArenaService.Services;

using System.Threading.Tasks;
using ArenaService.Models;
using ArenaService.Repositories;
using Libplanet.Crypto;

public interface IParticipateService
{
    Task<Participant> ParticipateAsync(int seasonId, Address avatarAddress);
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

    public async Task<Participant> ParticipateAsync(int seasonId, Address avatarAddress)
    {
        var existingParticipant = await _participantRepo.GetParticipantAsync(
            seasonId,
            avatarAddress
        );
        if (existingParticipant is not null)
        {
            return existingParticipant;
        }

        await _userRepo.GetUserAsync(avatarAddress);

        var participant = await _participantRepo.AddParticipantAsync(seasonId, avatarAddress);

        await _rankingRepo.UpdateScoreAsync(
            new Address(participant.AvatarAddress),
            seasonId,
            participant.Score
        );

        return participant;
    }
}
