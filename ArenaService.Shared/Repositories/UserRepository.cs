namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
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

    Task<User> UpdateUserAsync(Address avatarAddress, Action<User> updateFields);

    Task<User> UpdateUserAsync(User user, Action<User> updateFields);

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
        var query = _context.Users.AsQueryable().AsNoTracking();

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
        var query = _context.Users.AsQueryable().AsNoTracking();

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
        await _context.SaveChangesAsync();
        return user.Entity;
    }

    public async Task<User> UpdateUserAsync(Address avatarAddress, Action<User> updateFields)
    {
        var user = await GetUserAsync(avatarAddress);

        if (user is null)
        {
            throw new ArgumentException($"User not found for {avatarAddress}");
        }

        return await UpdateUserAsync(user, updateFields);
    }

    public async Task<User> UpdateUserAsync(User user, Action<User> updateFields)
    {
        updateFields(user);

        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return user;
    }
}
