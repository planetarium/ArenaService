namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IParticipantRepository
{
    Task<Participant> AddParticipantAsync(int seasonId, Address avatarAddress);
    Task AddParticipantsAsync(List<User> users, int seasonId);
    Task<Participant?> GetParticipantAsync(
        int seasonId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    );
    Task<List<Participant>> GetParticipantsAsync(
        int seasonId,
        int skip = 0,
        int take = 100,
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
        await _context.SaveChangesAsync();
        return participant.Entity;
    }

    public async Task AddParticipantsAsync(List<User> users, int seasonId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var participants = new List<Participant>();
            foreach (var user in users)
            {
                var participant = new Participant
                {
                    SeasonId = seasonId,
                    AvatarAddress = user.AvatarAddress,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                participants.Add(participant);
            }

            await _context.Participants.AddRangeAsync(participants);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Participant?> GetParticipantAsync(
        int seasonId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    )
    {
        var query = _context.Participants.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(p =>
            p.SeasonId == seasonId && p.AvatarAddress == avatarAddress
        );
    }

    public async Task<List<Participant>> GetParticipantsAsync(
        int seasonId,
        int skip = 0,
        int take = 100,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    )
    {
        var query = _context.Participants.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.Where(p => p.SeasonId == seasonId).Skip(skip).Take(take).ToListAsync();
        ;
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
