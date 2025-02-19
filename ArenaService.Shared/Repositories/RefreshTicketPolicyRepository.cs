namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models.RefreshTicket;
using Microsoft.EntityFrameworkCore;

public interface IRefreshTicketPolicyRepository
{
    Task<List<RefreshTicketPolicy>> GetAllRefreshPoliciesAsync();
    Task<RefreshTicketPolicy?> GetRefreshPolicyByIdAsync(int id);
    Task<RefreshTicketPolicy> AddRefreshPolicyAsync(RefreshTicketPolicy policy);
}

public class RefreshTicketPolicyRepository : IRefreshTicketPolicyRepository
{
    private readonly ArenaDbContext _context;

    public RefreshTicketPolicyRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<RefreshTicketPolicy>> GetAllRefreshPoliciesAsync()
    {
        return await _context.RefreshTicketPolicies.ToListAsync();
    }

    public async Task<RefreshTicketPolicy?> GetRefreshPolicyByIdAsync(int id)
    {
        return await _context.RefreshTicketPolicies.FindAsync(id);
    }

    public async Task<RefreshTicketPolicy> AddRefreshPolicyAsync(RefreshTicketPolicy policy)
    {
        var addedPolicy = await _context.RefreshTicketPolicies.AddAsync(policy);
        await _context.SaveChangesAsync();
        return addedPolicy.Entity;
    }
}
