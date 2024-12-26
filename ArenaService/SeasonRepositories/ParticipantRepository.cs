namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

public interface IParticipantRepository
{
    Task<Participant> InsertParticipantToSpecificSeasonAsync(
        int seasonId,
        string avatarAddress,
        string nameWithHash,
        int portraitId
    );
    Task<Participant?> GetParticipantByAvatarAddressAsync(int seasonId, string avatarAddress);
}

public class ParticipantRepository : IParticipantRepository
{
    private readonly ArenaDbContext _context;

    public ParticipantRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Participant> InsertParticipantToSpecificSeasonAsync(
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

    public async Task<Participant?> GetParticipantByAvatarAddressAsync(
        int seasonId,
        string avatarAddress
    )
    {
        return await _context.Participants.FirstOrDefaultAsync(p =>
            p.SeasonId == seasonId && p.AvatarAddress == avatarAddress
        );
    }
}
