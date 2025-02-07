using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_agent_address",
                table: "users");

            migrationBuilder.CreateTable(
                name: "clan_ranking_snapshots",
                columns: table => new
                {
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    clan_id = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ranking_snapshots",
                columns: table => new
                {
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ranking_snapshots", x => new { x.avatar_address, x.season_id, x.round_id });
                    table.ForeignKey(
                        name: "fk_ranking_snapshots_participants_avatar_address_season_id",
                        columns: x => new { x.avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ranking_snapshots_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ranking_snapshots_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_seasons_start_block",
                table: "seasons",
                column: "start_block");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_start_block_end_block",
                table: "seasons",
                columns: new[] { "start_block", "end_block" });

            migrationBuilder.CreateIndex(
                name: "ix_medals_medal_count_season_id",
                table: "medals",
                columns: new[] { "medal_count", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_clan_ranking_snapshots_round_id",
                table: "clan_ranking_snapshots",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_clan_ranking_snapshots_season_id_round_id",
                table: "clan_ranking_snapshots",
                columns: new[] { "season_id", "round_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ranking_snapshots_round_id",
                table: "ranking_snapshots",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_ranking_snapshots_season_id_round_id",
                table: "ranking_snapshots",
                columns: new[] { "season_id", "round_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clan_ranking_snapshots");

            migrationBuilder.DropTable(
                name: "ranking_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_seasons_start_block",
                table: "seasons");

            migrationBuilder.DropIndex(
                name: "ix_seasons_start_block_end_block",
                table: "seasons");

            migrationBuilder.DropIndex(
                name: "ix_medals_medal_count_season_id",
                table: "medals");

            migrationBuilder.CreateIndex(
                name: "ix_users_agent_address",
                table: "users",
                column: "agent_address",
                unique: true);
        }
    }
}
