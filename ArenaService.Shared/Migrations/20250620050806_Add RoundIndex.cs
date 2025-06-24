using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rounds_season_id",
                table: "rounds");

            migrationBuilder.AddColumn<int>(
                name: "round_index",
                table: "rounds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_rounds_season_id_round_index",
                table: "rounds",
                columns: new[] { "season_id", "round_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_battles_round_id",
                table: "battles",
                column: "round_id");

            migrationBuilder.AddForeignKey(
                name: "fk_battles_rounds_round_id",
                table: "battles",
                column: "round_id",
                principalTable: "rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_battles_rounds_round_id",
                table: "battles");

            migrationBuilder.DropIndex(
                name: "ix_rounds_season_id_round_index",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "ix_battles_round_id",
                table: "battles");

            migrationBuilder.DropColumn(
                name: "round_index",
                table: "rounds");

            migrationBuilder.CreateIndex(
                name: "ix_rounds_season_id",
                table: "rounds",
                column: "season_id");
        }
    }
}
