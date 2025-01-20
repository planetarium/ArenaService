using ArenaService.Models;
using ArenaService.Models.Ticket;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Data;

public class ArenaDbContext : DbContext
{
    public ArenaDbContext(DbContextOptions<ArenaDbContext> options)
        : base(options) { }

    public required DbSet<User> Users { get; set; }
    public required DbSet<Season> Seasons { get; set; }
    public required DbSet<Round> Rounds { get; set; }
    public required DbSet<Participant> Participants { get; set; }
    public required DbSet<TicketPolicy> TicketPolicies { get; set; }
    public required DbSet<TicketPurchaseLog> TicketPurchaseLogs { get; set; }
    public required DbSet<TicketStatus> TicketStatuses { get; set; }
    public required DbSet<TicketUsageLog> TicketUsageLogs { get; set; }
    public required DbSet<Battle> Battles { get; set; }
    public required DbSet<AvailableOpponent> AvailableOpponents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<TicketPolicy>()
            .HasOne(tp => tp.Season)
            .WithMany(s => s.TicketPolicies)
            .HasForeignKey(tp => tp.SeasonId);

        modelBuilder
            .Entity<Round>()
            .HasOne(r => r.Season)
            .WithMany(s => s.Rounds)
            .HasForeignKey(r => r.SeasonId);

        modelBuilder.Entity<Participant>().HasKey(p => new { p.AvatarAddress, p.SeasonId });

        modelBuilder
            .Entity<Participant>()
            .HasOne(p => p.Season)
            .WithMany()
            .HasForeignKey(p => p.SeasonId);

        modelBuilder
            .Entity<TicketStatus>()
            .HasOne(ts => ts.Round)
            .WithMany()
            .HasForeignKey(ts => ts.RoundId);

        modelBuilder
            .Entity<TicketStatus>()
            .HasOne(ts => ts.Participant)
            .WithMany()
            .HasForeignKey(b => new { b.AvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Me)
            .WithMany(p => p.AvailableOpponents)
            .HasForeignKey(ao => new { ao.AvatarAddress, ao.SeasonId });

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Opponent)
            .WithMany()
            .HasForeignKey(ao => new { ao.OpponentAvatarAddress, ao.SeasonId });

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Opponent)
            .WithMany()
            .HasForeignKey(ao => new { ao.OpponentAvatarAddress, ao.SeasonId });

    }
}
