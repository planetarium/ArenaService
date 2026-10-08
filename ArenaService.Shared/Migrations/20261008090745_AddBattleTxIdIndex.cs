using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleTxIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ix_battles_tx_id was created manually in production (odin/heimdall) on 2026-10-08
            // with CREATE INDEX CONCURRENTLY, so this must be idempotent.
            // CONCURRENTLY cannot run inside a transaction, hence suppressTransaction.
            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_battles_tx_id ON battles (tx_id);",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_battles_tx_id;",
                suppressTransaction: true);
        }
    }
}
