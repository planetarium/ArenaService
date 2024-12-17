using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Data;

public class ArenaDbContext : DbContext
{
    public ArenaDbContext(DbContextOptions<ArenaDbContext> options)
        : base(options) { }

    public required DbSet<Participant> Participants { get; set; }
    public required DbSet<BattleLog> BattleLogs { get; set; }
    public required DbSet<Season> Seasons { get; set; }
    public required DbSet<LeaderboardEntry> Leaderboard { get; set; }
    public required DbSet<AvailableOpponent> AvailableOpponents { get; set; }

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
            .HasForeignKey(b => b.OpponentId);

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Season)
            .WithMany(s => s.BattleLogs)
            .HasForeignKey(b => b.SeasonId);

        modelBuilder
            .Entity<LeaderboardEntry>()
            .HasOne(le => le.Participant)
            .WithMany(p => p.Leaderboard)
            .HasForeignKey(le => le.ParticipantId);

        modelBuilder
            .Entity<LeaderboardEntry>()
            .HasOne(le => le.Season)
            .WithMany(s => s.Leaderboard)
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
            .HasForeignKey(ao => ao.OpponentId);
    }
}
