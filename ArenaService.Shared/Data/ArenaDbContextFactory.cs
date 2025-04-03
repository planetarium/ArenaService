using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ArenaService.Shared.Data;

public class ArenaDbContextFactory : IDesignTimeDbContextFactory<ArenaDbContext>
{
    public ArenaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<ArenaDbContext>();
        optionsBuilder.UseNpgsql(connectionString, 
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("ArenaService.Shared"))
            .UseSnakeCaseNamingConvention();

        return new ArenaDbContext(optionsBuilder.Options)
        {
            Users = null!,
            Clans = null!,
            Seasons = null!,
            Rounds = null!,
            Medals = null!,
            Participants = null!,
            RankingSnapshots = null!,
            BattleTicketPolicies = null!,
            BattleTicketPurchaseLogs = null!,
            BattleTicketUsageLogs = null!,
            BattleTicketStatusesPerRound = null!,
            BattleTicketStatusesPerSeason = null!,
            RefreshTicketPolicies = null!,
            RefreshTicketPurchaseLogs = null!,
            RefreshTicketStatusesPerRound = null!,
            RefreshTicketUsageLogs = null!,
            Battles = null!,
            AvailableOpponents = null!
        };
    }
} 