namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IUserRepository
{
    Task<User> AddUserAsync(
        Address agentAddress,
        Address avatarAddress,
        string nameWithHash,
        int portraitId,
        long cp,
        int level
    );

    Task<User> AddOrGetUserAsync(
        Address agentAddress,
        Address avatarAddress,
        string nameWithHash,
        int portraitId,
        long cp,
        int level
    );

    Task<User?> GetUserAsync(
        Address avatarAddress,
        Func<IQueryable<User>, IQueryable<User>>? includeQuery = null
    );

    Task<List<User>> GetAllUserAsync(Func<IQueryable<User>, IQueryable<User>>? includeQuery = null);
}

public class UserRepository : IUserRepository
{
    private readonly ArenaDbContext _context;

    public UserRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<User> AddOrGetUserAsync(
        Address agentAddress,
        Address avatarAddress,
        string nameWithHash,
        int portraitId,
        long cp,
        int level
    )
    {
        var existingUser = await _context.Users.FindAsync(avatarAddress);

        if (existingUser != null)
        {
            return existingUser;
        }

        var newUser = await AddUserAsync(
            agentAddress,
            avatarAddress,
            nameWithHash,
            portraitId,
            cp,
            level
        );

        return newUser;
    }

    public async Task<User?> GetUserAsync(
        Address avatarAddress,
        Func<IQueryable<User>, IQueryable<User>>? includeQuery = null
    )
    {
        var query = _context.Users.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(u => u.AvatarAddress == avatarAddress);
    }

    public async Task<List<User>> GetAllUserAsync(
        Func<IQueryable<User>, IQueryable<User>>? includeQuery = null
    )
    {
        var query = _context.Users.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.ToListAsync();
    }

    public async Task<User> AddUserAsync(
        Address agentAddress,
        Address avatarAddress,
        string nameWithHash,
        int portraitId,
        long cp,
        int level
    )
    {
        var user = await _context.Users.AddAsync(
            new User
            {
                AgentAddress = agentAddress,
                AvatarAddress = avatarAddress,
                NameWithHash = nameWithHash,
                PortraitId = portraitId,
                Cp = cp,
                Level = level
            }
        );
        _context.SaveChanges();
        return user.Entity;
    }
}
