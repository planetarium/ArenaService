namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using EFCore.BulkExtensions;
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
    Task<int> GetParticipantCountAsync(int seasonId);
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
    Task<bool> UpdateMyScore(
        int seasonId,
        Address avatarAddress,
        int scoreChange,
        bool isVictory
    );
    Task<bool> UpdateOpponentScore(
        int seasonId,
        Address avatarAddress,
        int scoreChange
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

            await _context.BulkInsertAsync(participants);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> GetParticipantCountAsync(int seasonId)
    {
        return await _context
            .RankingSnapshots.AsQueryable()
            .AsNoTracking()
            .Where(p => p.SeasonId == seasonId)
            .CountAsync();
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

    public async Task<bool> UpdateMyScore(
        int seasonId,
        Address avatarAddress,
        int scoreChange,
        bool isVictory
    )
    {
        var query = _context.Participants
            .Where(p => p.SeasonId == seasonId && p.AvatarAddress == avatarAddress);

        var affected = await query.ExecuteUpdateAsync(p => p
            .SetProperty(x => x.Score, x => x.Score + scoreChange)
            .SetProperty(x => x.TotalWin, x => x.TotalWin + (isVictory ? 1 : 0))
            .SetProperty(x => x.TotalLose, x => x.TotalLose + (isVictory ? 0 : 1))
            .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
        );
        return affected > 0;
    }

    public async Task<bool> UpdateOpponentScore(
        int seasonId,
        Address avatarAddress,
        int scoreChange
    )
    {
        var query = _context.Participants
            .Where(p => p.SeasonId == seasonId && p.AvatarAddress == avatarAddress);

        var affected = await query.ExecuteUpdateAsync(p => p
            .SetProperty(x => x.Score, x => x.Score + scoreChange)
            .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
        );
        return affected > 0;
    }
}
