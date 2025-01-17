using ArenaService.Models;
using ArenaService.Views;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Data;

public class ArenaDbContext : DbContext
{
    public ArenaDbContext(DbContextOptions<ArenaDbContext> options)
        : base(options) { }

    public required DbSet<User> Users { get; set; }
    public required DbSet<Season> Seasons { get; set; }
    public required DbSet<Round> Rounds { get; set; }
    public required DbSet<RefreshRequest> RefreshRequests { get; set; }
    public required DbSet<RefreshPricePolicy> RefreshPricePolicies { get; set; }
    public required DbSet<RefreshPriceDetail> RefreshPriceDetails { get; set; }
    public required DbSet<Participant> Participants { get; set; }
    public required DbSet<BattleLog> BattleLogs { get; set; }
    public required DbSet<AvailableOpponent> AvailableOpponents { get; set; }
    public required DbSet<AvailableOpponentsRefreshRequest> AvailableOpponentsRefreshRequests { get; set; }
    public required DbSet<RefreshPriceMaterializedView> RefreshPriceView { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Participant>().HasKey(p => new { p.AvatarAddress, p.SeasonId });

        modelBuilder
            .Entity<Round>()
            .HasOne(r => r.Season)
            .WithMany(s => s.Rounds)
            .HasForeignKey(r => r.SeasonId);

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Attacker)
            .WithMany()
            .HasForeignKey(b => new { b.AttackerAvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Defender)
            .WithMany()
            .HasForeignKey(b => new { b.DefenderAvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(b => b.MyParticipant)
            .WithMany()
            .HasForeignKey(b => new { b.AvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(b => b.Opponent)
            .WithMany()
            .HasForeignKey(b => new { b.OpponentAvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<RefreshPriceMaterializedView>()
            .ToView(RefreshPriceMaterializedView.ViewName)
            .HasNoKey();
    }
}
