namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

public interface IAvailableOpponentRepository
{
    Task<List<AvailableOpponent>> GetAvailableOpponents(int participantId);
}

public class AvailableOpponentRepository : IAvailableOpponentRepository
{
    private readonly ArenaDbContext _context;

    public AvailableOpponentRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<AvailableOpponent>> GetAvailableOpponents(int participantId)
    {
        return await _context
            .AvailableOpponents.Where(ao => ao.ParticipantId == participantId)
            .ToListAsync();
    }
}
