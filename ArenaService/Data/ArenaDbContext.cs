using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Data;

public class ArenaDbContext : DbContext
{
    public ArenaDbContext(DbContextOptions<ArenaDbContext> options)
        : base(options) { }

    public DbSet<Participant> Participants { get; set; }
    public DbSet<BattleLog> BattleLogs { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }
    public DbSet<AvailableOpponent> AvailableOpponents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Participant)
            .WithMany(p => p.BattleLogs)
            .HasForeignKey(b => b.ParticipantId);

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Opponent)
            .WithMany()
            .HasForeignKey(b => b.OpponentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Season)
            .WithMany(s => s.BattleLogs)
            .HasForeignKey(b => b.SeasonId);

        modelBuilder
            .Entity<LeaderboardEntry>()
            .HasOne(le => le.Participant)
            .WithMany(p => p.LeaderboardEntries)
            .HasForeignKey(le => le.ParticipantId);

        modelBuilder
            .Entity<LeaderboardEntry>()
            .HasOne(le => le.Season)
            .WithMany(s => s.LeaderboardEntries)
            .HasForeignKey(le => le.SeasonId);

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Participant)
            .WithMany()
            .HasForeignKey(ao => ao.ParticipantId);

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Opponent)
            .WithMany()
            .HasForeignKey(ao => ao.OpponentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Season)
            .WithMany()
            .HasForeignKey(ao => ao.SeasonId);
    }
}
