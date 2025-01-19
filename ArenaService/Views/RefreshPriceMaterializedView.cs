using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Views;

public class RefreshPriceMaterializedView
{
    public const string ViewName = "refresh_price_view";

    public int DetailId { get; set; }

    public int SeasonId { get; set; }
    public int PolicyId { get; set; }
    public int RefreshOrder { get; set; }

    public double Price { get; set; }

    public static async Task RefreshMaterializedViewAsync(DbContext dbContext)
    {
        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"REFRESH MATERIALIZED VIEW {ViewName}";
        command.CommandType = CommandType.Text;

        await dbContext.Database.OpenConnectionAsync();
        await command.ExecuteNonQueryAsync();
        await dbContext.Database.CloseConnectionAsync();
    }

    public static async Task<bool> MaterializedViewExistsAsync(DbContext dbContext)
    {
        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            $"SELECT EXISTS (SELECT 1 FROM pg_matviews WHERE matviewname = '{ViewName}');";
        command.CommandType = CommandType.Text;

        await dbContext.Database.OpenConnectionAsync();
        var result = await command.ExecuteScalarAsync();
        await dbContext.Database.CloseConnectionAsync();

        return result != null && (bool)result;
    }

    public static async Task CreateMaterializedViewAsync(DbContext dbContext)
    {
        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            @$"
            CREATE MATERIALIZED VIEW IF NOT EXISTS {ViewName} AS
            SELECT
                s.id AS season_id,
                p.id AS policy_id,
                d.id AS detail_id,
                d.refresh_order AS refresh_order,
                d.price AS price
            FROM 
                seasons s
            JOIN 
                refresh_price_policies p ON s.price_policy_id = p.id
            JOIN 
                refresh_price_details d ON p.id = d.policy_id;";
        command.CommandType = CommandType.Text;

        await dbContext.Database.OpenConnectionAsync();
        await command.ExecuteNonQueryAsync();
        await dbContext.Database.CloseConnectionAsync();
    }

    public static async Task InitializeMaterializedViewAsync(DbContext dbContext)
    {
        if (!await MaterializedViewExistsAsync(dbContext))
        {
            await CreateMaterializedViewAsync(dbContext);
        }

        await RefreshMaterializedViewAsync(dbContext);
    }
}
