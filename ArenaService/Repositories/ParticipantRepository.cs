namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IParticipantRepository
{
    Task<Participant> AddParticipantAsync(int seasonId, Address avatarAddress);
    Task<Participant?> GetParticipantAsync(
        int seasonId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    );
    Task<Participant> UpdateParticipantAsync(
        int seasonId,
        Address avatarAddress,
        Action<Participant> updateFields
    );
    Task<Participant> UpdateParticipantAsync(
        Participant participant,
        Action<Participant> updateFields
    );
}

public class ParticipantRepository : IParticipantRepository
{
    private readonly ArenaDbContext _context;

    public ParticipantRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Participant> AddParticipantAsync(int seasonId, Address avatarAddress)
    {
        var participant = await _context.Participants.AddAsync(
            new Participant { AvatarAddress = avatarAddress, SeasonId = seasonId }
        );
        _context.SaveChanges();
        return participant.Entity;
    }

    public async Task<Participant?> GetParticipantAsync(
        int seasonId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    )
    {
        var query = _context.Participants.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(p =>
            p.SeasonId == seasonId && p.AvatarAddress == avatarAddress
        );
    }

    public async Task<Participant> UpdateParticipantAsync(
        int seasonId,
        Address avatarAddress,
        Action<Participant> updateFields
    )
    {
        var participant = await GetParticipantAsync(seasonId, avatarAddress);

        if (participant is null)
        {
            throw new ArgumentException($"Participant not found for {seasonId}, {avatarAddress}");
        }

        return await UpdateParticipantAsync(participant, updateFields);
    }

    public async Task<Participant> UpdateParticipantAsync(
        Participant participant,
        Action<Participant> updateFields
    )
    {
        updateFields(participant);

        participant.UpdatedAt = DateTime.UtcNow;

        _context.Participants.Update(participant);
        await _context.SaveChangesAsync();

        return participant;
    }
}
