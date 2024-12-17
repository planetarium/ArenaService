using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArenaService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_block_index = table.Column<long>(type: "bigint", nullable: false),
                    end_block_index = table.Column<long>(type: "bigint", nullable: false),
                    ticket_refill_interval = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatar_address = table.Column<string>(type: "text", nullable: false),
                    name_with_hash = table.Column<string>(type: "text", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    cp = table.Column<int>(type: "integer", nullable: false),
                    portrait_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_participants_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "available_opponents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participant_id = table.Column<int>(type: "integer", nullable: false),
                    opponent_id = table.Column<int>(type: "integer", nullable: false),
                    refill_block_index = table.Column<long>(type: "bigint", nullable: false),
                    is_battled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_available_opponents", x => x.id);
                    table.ForeignKey(
                        name: "fk_available_opponents_participants_opponent_id",
                        column: x => x.opponent_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_available_opponents_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "battle_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participant_id = table.Column<int>(type: "integer", nullable: false),
                    opponent_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    battle_block_index = table.Column<long>(type: "bigint", nullable: false),
                    is_victory = table.Column<bool>(type: "boolean", nullable: false),
                    participant_score_change = table.Column<int>(type: "integer", nullable: false),
                    opponent_score_change = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_battle_logs_participants_opponent_id",
                        column: x => x.opponent_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_logs_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_logs_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leaderboard",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participant_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    total_score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leaderboard", x => x.id);
                    table.ForeignKey(
                        name: "fk_leaderboard_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_leaderboard_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_opponent_id",
                table: "available_opponents",
                column: "opponent_id");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_participant_id",
                table: "available_opponents",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_logs_opponent_id",
                table: "battle_logs",
                column: "opponent_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_logs_participant_id",
                table: "battle_logs",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_logs_season_id",
                table: "battle_logs",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_participant_id",
                table: "leaderboard",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_season_id",
                table: "leaderboard",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_avatar_address",
                table: "participants",
                column: "avatar_address");

            migrationBuilder.CreateIndex(
                name: "ix_participants_season_id",
                table: "participants",
                column: "season_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "available_opponents");

            migrationBuilder.DropTable(
                name: "battle_logs");

            migrationBuilder.DropTable(
                name: "leaderboard");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "seasons");
        }
    }
}
