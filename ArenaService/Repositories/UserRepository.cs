namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;

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

    Task<User?> GetUserAsync(Address avatarAddress);
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
        var existingUser = await _context.Users.FindAsync(avatarAddress.ToHex());

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

    public async Task<User?> GetUserAsync(Address avatarAddress)
    {
        var user = await _context.Users.FindAsync(avatarAddress.ToHex());
        return user;
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
