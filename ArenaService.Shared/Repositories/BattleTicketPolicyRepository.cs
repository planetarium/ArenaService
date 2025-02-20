namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models.BattleTicket;
using Microsoft.EntityFrameworkCore;

public interface IBattleTicketPolicyRepository
{
    Task<List<BattleTicketPolicy>> GetAllBattlePoliciesAsync();
    Task<BattleTicketPolicy?> GetBattlePolicyByIdAsync(int id);
    Task<BattleTicketPolicy> AddBattlePolicyAsync(BattleTicketPolicy policy);
}

public class BattleTicketPolicyRepository : IBattleTicketPolicyRepository
{
    private readonly ArenaDbContext _context;

    public BattleTicketPolicyRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<BattleTicketPolicy>> GetAllBattlePoliciesAsync()
    {
        return await _context.BattleTicketPolicies.ToListAsync();
    }

    public async Task<BattleTicketPolicy?> GetBattlePolicyByIdAsync(int id)
    {
        return await _context.BattleTicketPolicies.FindAsync(id);
    }

    public async Task<BattleTicketPolicy> AddBattlePolicyAsync(BattleTicketPolicy policy)
    {
        var addedPolicy = await _context.BattleTicketPolicies.AddAsync(policy);
        await _context.SaveChangesAsync();
        return addedPolicy.Entity;
    }
}
