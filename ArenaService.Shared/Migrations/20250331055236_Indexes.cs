using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Migrations
{
    /// <inheritdoc />
    public partial class Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_available_opponents_round_id",
                table: "available_opponents");

            migrationBuilder.CreateIndex(
                name: "ix_ranking_snapshots_season_id",
                table: "ranking_snapshots",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_season_id_score",
                table: "participants",
                columns: new[] { "season_id", "score" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_battles_battle_status_reviewed",
                table: "battles",
                columns: new[] { "battle_status", "reviewed" });

            migrationBuilder.CreateIndex(
                name: "ix_battles_id_tx_id",
                table: "battles",
                columns: new[] { "id", "tx_id" });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_avatar_address_round_id_deleted_at",
                table: "available_opponents",
                columns: new[] { "avatar_address", "round_id", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_round_id_avatar_address_opponent_avatar",
                table: "available_opponents",
                columns: new[] { "round_id", "avatar_address", "opponent_avatar_address", "success_battle_id", "deleted_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ranking_snapshots_season_id",
                table: "ranking_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_participants_season_id_score",
                table: "participants");

            migrationBuilder.DropIndex(
                name: "ix_battles_battle_status_reviewed",
                table: "battles");

            migrationBuilder.DropIndex(
                name: "ix_battles_id_tx_id",
                table: "battles");

            migrationBuilder.DropIndex(
                name: "ix_available_opponents_avatar_address_round_id_deleted_at",
                table: "available_opponents");

            migrationBuilder.DropIndex(
                name: "ix_available_opponents_round_id_avatar_address_opponent_avatar",
                table: "available_opponents");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_round_id",
                table: "available_opponents",
                column: "round_id");
        }
    }
}
