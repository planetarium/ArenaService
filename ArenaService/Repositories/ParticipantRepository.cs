namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IParticipantRepository
{
    Task<Participant> AddParticipantAsync(int seasonId, Address avatarAddress);
    Task<Participant> GetParticipantAsync(
        int seasonId,
        Address avatarAddress,
        Func<IQueryable<Participant>, IQueryable<Participant>>? includeQuery = null
    );
    Task<Participant> UpdateScoreAsync(int seasonId, Address avatarAddress, int scoreChange);
    Task<Participant> UpdateLastRefreshRequestId(
        int seasonId,
        Address avatarAddress,
        int refreshRequestId
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
            new Participant { AvatarAddress = avatarAddress.ToHex(), SeasonId = seasonId }
        );
        _context.SaveChanges();
        return participant.Entity;
    }

    public async Task<Participant> GetParticipantAsync(
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

        return await query.FirstAsync(p =>
            p.SeasonId == seasonId && p.AvatarAddress == avatarAddress.ToHex()
        );
    }

    public async Task<Participant> UpdateScoreAsync(
        int seasonId,
        Address avatarAddress,
        int scoreChange
    )
    {
        var participant = await _context.Participants.FirstOrDefaultAsync(p =>
            p.SeasonId == seasonId && p.AvatarAddress == avatarAddress.ToHex()
        );
        ;
        if (participant == null)
        {
            throw new KeyNotFoundException($"Participants {seasonId}, {avatarAddress} not found.");
        }

        participant.Score += scoreChange;

        _context.Participants.Update(participant);
        await _context.SaveChangesAsync();

        return participant;
    }

    public async Task<Participant> UpdateLastRefreshRequestId(
        int seasonId,
        Address avatarAddress,
        int refreshRequestId
    )
    {
        var participant = await _context.Participants.FirstOrDefaultAsync(p =>
            p.SeasonId == seasonId && p.AvatarAddress == avatarAddress.ToHex()
        );

        if (participant == null)
        {
            throw new KeyNotFoundException($"Participants {seasonId}, {avatarAddress} not found.");
        }

        participant.LastRefreshRequestId = refreshRequestId;

        _context.Participants.Update(participant);
        await _context.SaveChangesAsync();

        return participant;
    }
}
