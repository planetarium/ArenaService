namespace ArenaService.Services;

using System.Threading.Tasks;
using ArenaService.Dtos;
using ArenaService.Repositories;
using Libplanet.Crypto;

public class RegistrationService
{
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonRepository _seasonRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRankingRepository _rankingRepo;

    public RegistrationService(
        IParticipantRepository participantRepo,
        ISeasonRepository seasonRepo,
        IUserRepository userRepo,
        IRankingRepository rankingRepo
    )
    {
        _participantRepo = participantRepo;
        _seasonRepo = seasonRepo;
        _userRepo = userRepo;
        _rankingRepo = rankingRepo;
    }

    public async Task<bool> EnsureUserRegisteredAsync(
        int seasonId,
        Address avatarAddress,
        Address agentAddress,
        ParticipateRequest participateRequest
    )
    {
        var season = await _seasonRepo.GetSeasonAsync(seasonId);
        if (season is null)
        {
            throw new KeyNotFoundException($"Season {seasonId} not found.");
        }

        var existingParticipant = await _participantRepo.GetParticipantAsync(
            seasonId,
            avatarAddress
        );
        if (existingParticipant is not null)
        {
            return false;
        }

        await _userRepo.AddOrGetUserAsync(
            agentAddress,
            avatarAddress,
            participateRequest.NameWithHash,
            participateRequest.PortraitId,
            participateRequest.Cp,
            participateRequest.Level
        );

        var participant = await _participantRepo.AddParticipantAsync(seasonId, avatarAddress);

        await _rankingRepo.UpdateScoreAsync(
            new Address(participant.AvatarAddress),
            seasonId,
            participant.Score
        );

        return true;
    }
}
