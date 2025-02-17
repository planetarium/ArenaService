using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Migrations
{
    /// <inheritdoc />
    public partial class AddClanIdAtSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clan_ranking_snapshots");

            migrationBuilder.AddColumn<int>(
                name: "clan_id",
                table: "ranking_snapshots",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "clan_id",
                table: "ranking_snapshots");

            migrationBuilder.CreateTable(
                name: "clan_ranking_snapshots",
                columns: table => new
                {
                    clan_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clan_ranking_snapshots", x => new { x.clan_id, x.season_id, x.round_id });
                    table.ForeignKey(
                        name: "fk_clan_ranking_snapshots_clans_clan_id",
                        column: x => x.clan_id,
                        principalTable: "clans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_clan_ranking_snapshots_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_clan_ranking_snapshots_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clan_ranking_snapshots_round_id",
                table: "clan_ranking_snapshots",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_clan_ranking_snapshots_season_id_round_id",
                table: "clan_ranking_snapshots",
                columns: new[] { "season_id", "round_id" });
        }
    }
}
