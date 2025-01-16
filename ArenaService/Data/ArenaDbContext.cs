using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Data;

public class ArenaDbContext : DbContext
{
    public ArenaDbContext(DbContextOptions<ArenaDbContext> options)
        : base(options) { }

    public required DbSet<User> Users { get; set; }
    public required DbSet<Season> Seasons { get; set; }
    public required DbSet<Participant> Participants { get; set; }
    public required DbSet<BattleLog> BattleLogs { get; set; }
    public required DbSet<AvailableOpponents> AvailableOpponents { get; set; }
    public required DbSet<AvailableOpponentsRequest> AvailableOpponentsRequests { get; set; }
    public required DbSet<Round> Rounds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Participant>().HasKey(p => new { p.AvatarAddress, p.SeasonId });

        modelBuilder
            .Entity<Participant>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.AvatarAddress)
            .HasPrincipalKey(u => u.AvatarAddress);

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Defender)
            .WithMany()
            .HasForeignKey(b => new { b.DefenderAvatarAddress, b.SeasonId })
            .HasPrincipalKey(p => new { p.AvatarAddress, p.SeasonId });

        modelBuilder
            .Entity<BattleLog>()
            .HasOne(b => b.Attacker)
            .WithMany()
            .HasForeignKey(b => new { b.AttackerAvatarAddress, b.SeasonId })
            .HasPrincipalKey(p => new { p.AvatarAddress, p.SeasonId });

        modelBuilder
            .Entity<AvailableOpponents>()
            .HasKey(ao => new { ao.AvatarAddress, ao.RoundId });

        modelBuilder
            .Entity<AvailableOpponents>()
            .HasOne(ao => ao.Round)
            .WithMany()
            .HasForeignKey(ao => ao.RoundId)
            .HasPrincipalKey(r => r.Id);

        modelBuilder
            .Entity<AvailableOpponentsRequest>()
            .HasOne(ao => ao.Round)
            .WithMany()
            .HasForeignKey(ao => ao.RoundId)
            .HasPrincipalKey(r => r.Id);

        modelBuilder
            .Entity<Round>()
            .HasOne(ai => ai.Season)
            .WithMany(s => s.Rounds)
            .HasForeignKey(r => r.SeasonId);
    }
}
