using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

public class ArenaParticipantContext : DbContext
{
    public ArenaParticipantContext()
    {
    }

    public ArenaParticipantContext(DbContextOptions<ArenaParticipantContext> options) : base(options)
    {
    }

    public DbSet<ArenaParticipant> ArenaParticipants { get; set; } = null;
}