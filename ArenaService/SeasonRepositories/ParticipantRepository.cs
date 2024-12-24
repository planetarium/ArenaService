namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;

public interface IParticipantRepository
{
    Task<Participant> InsertParticipantToSpecificSeason(
        int seasonId,
        string avatarAddress,
        string nameWithHash,
        int portraitId
    );
}

public class ParticipantRepository : IParticipantRepository
{
    private readonly ArenaDbContext _context;

    public ParticipantRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Participant> InsertParticipantToSpecificSeason(
        int seasonId,
        string avatarAddress,
        string nameWithHash,
        int portraitId
    )
    {
        var participant = await _context.Participants.AddAsync(
            new Participant
            {
                AvatarAddress = avatarAddress,
                NameWithHash = nameWithHash,
                PortraitId = portraitId,
                SeasonId = seasonId
            }
        );
        _context.SaveChanges();
        return participant.Entity;
    }
}
