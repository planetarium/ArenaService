using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.Converters;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Models.Ticket;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Data;

public class ArenaDbContext : DbContext
{
    public ArenaDbContext(DbContextOptions<ArenaDbContext> options)
        : base(options) { }

    public required DbSet<User> Users { get; set; }
    public required DbSet<Clan> Clans { get; set; }
    public required DbSet<Season> Seasons { get; set; }
    public required DbSet<Round> Rounds { get; set; }
    public required DbSet<Medal> Medals { get; set; }
    public required DbSet<Participant> Participants { get; set; }
    public required DbSet<RankingSnapshot> RankingSnapshots { get; set; }
    public required DbSet<BattleTicketPolicy> BattleTicketPolicies { get; set; }
    public required DbSet<BattleTicketPurchaseLog> BattleTicketPurchaseLogs { get; set; }
    public required DbSet<BattleTicketUsageLog> BattleTicketUsageLogs { get; set; }
    public required DbSet<BattleTicketStatusPerRound> BattleTicketStatusesPerRound { get; set; }
    public required DbSet<BattleTicketStatusPerSeason> BattleTicketStatusesPerSeason { get; set; }
    public required DbSet<RefreshTicketPolicy> RefreshTicketPolicies { get; set; }
    public required DbSet<RefreshTicketPurchaseLog> RefreshTicketPurchaseLogs { get; set; }
    public required DbSet<RefreshTicketStatusPerRound> RefreshTicketStatusesPerRound { get; set; }
    public required DbSet<RefreshTicketUsageLog> RefreshTicketUsageLogs { get; set; }
    public required DbSet<Battle> Battles { get; set; }
    public required DbSet<AvailableOpponent> AvailableOpponents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<User>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<User>()
            .Property(p => p.AgentAddress)
            .HasConversion(new AddressConverter());

        modelBuilder.Entity<User>().HasOne(u => u.Clan).WithMany().HasForeignKey(u => u.ClanId);

        modelBuilder
            .Entity<Round>()
            .HasOne(r => r.Season)
            .WithMany(s => s.Rounds)
            .HasForeignKey(r => r.SeasonId);

        modelBuilder
            .Entity<Participant>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder.Entity<Participant>().HasKey(p => new { p.AvatarAddress, p.SeasonId });

        modelBuilder
            .Entity<Participant>()
            .HasOne(p => p.Season)
            .WithMany()
            .HasForeignKey(p => p.SeasonId);

        modelBuilder
            .Entity<BattleTicketStatusPerRound>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<BattleTicketStatusPerRound>()
            .HasOne(ts => ts.Round)
            .WithMany()
            .HasForeignKey(ts => ts.RoundId);

        modelBuilder
            .Entity<BattleTicketStatusPerRound>()
            .HasOne(ts => ts.Participant)
            .WithMany()
            .HasForeignKey(b => new { b.AvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<BattleTicketStatusPerRound>()
            .HasCheckConstraint("CK_BattleTicketStatusPerRound_RemainingCount", "remaining_count >= 0");

        modelBuilder
            .Entity<RefreshTicketStatusPerRound>()
            .HasCheckConstraint("CK_RefreshTicketStatusPerRound_RemainingCount", "remaining_count >= 0");

        modelBuilder
            .Entity<BattleTicketStatusPerSeason>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<BattleTicketStatusPerSeason>()
            .HasOne(ts => ts.Season)
            .WithMany()
            .HasForeignKey(ts => ts.SeasonId);

        modelBuilder
            .Entity<BattleTicketStatusPerSeason>()
            .HasOne(ts => ts.Participant)
            .WithMany()
            .HasForeignKey(b => new { b.AvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<BattleTicketPurchaseLog>()
            .Property(ts => ts.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<BattleTicketPurchaseLog>()
            .Property(ts => ts.TxId)
            .HasConversion(new TxIdConverter());

        modelBuilder
            .Entity<RefreshTicketStatusPerRound>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<RefreshTicketStatusPerRound>()
            .HasOne(ts => ts.Round)
            .WithMany()
            .HasForeignKey(ts => ts.RoundId);

        modelBuilder
            .Entity<RefreshTicketStatusPerRound>()
            .HasOne(ts => ts.Participant)
            .WithMany()
            .HasForeignKey(b => new { b.AvatarAddress, b.SeasonId });

        modelBuilder
            .Entity<RefreshTicketStatusPerRound>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<RefreshTicketPurchaseLog>()
            .Property(ts => ts.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<RefreshTicketPurchaseLog>()
            .Property(ts => ts.TxId)
            .HasConversion(new TxIdConverter());

        modelBuilder
            .Entity<AvailableOpponent>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Me)
            .WithMany(p => p.AvailableOpponents)
            .HasForeignKey(ao => new { ao.AvatarAddress, ao.SeasonId });

        modelBuilder
            .Entity<AvailableOpponent>()
            .Property(p => p.OpponentAvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<AvailableOpponent>()
            .HasOne(ao => ao.Opponent)
            .WithMany()
            .HasForeignKey(ao => new { ao.OpponentAvatarAddress, ao.SeasonId });

        modelBuilder
            .Entity<Battle>()
            .HasOne(b => b.AvailableOpponent)
            .WithMany(ao => ao.Battles)
            .HasForeignKey(b => b.AvailableOpponentId);

        modelBuilder
            .Entity<Battle>()
            .Property(p => p.AvatarAddress)
            .HasConversion(new AddressConverter());

        modelBuilder
            .Entity<Battle>()
            .HasOne(b => b.Participant)
            .WithMany()
            .HasForeignKey(b => new { b.AvatarAddress, b.SeasonId });

        modelBuilder.Entity<Battle>().Property(ts => ts.TxId).HasConversion(new TxIdConverter());

        modelBuilder
            .Entity<User>()
            .HasOne(u => u.Clan)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.ClanId);
    }
}
