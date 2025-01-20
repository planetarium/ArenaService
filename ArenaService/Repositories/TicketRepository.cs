namespace ArenaService.Repositories;

using ArenaService.Constants;
using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;

public interface ITicketRepository
{
}

public class TicketRepository : ITicketRepository
{
    private readonly ArenaDbContext _context;

    public TicketRepository(ArenaDbContext context)
    {
        _context = context;
    }

}
