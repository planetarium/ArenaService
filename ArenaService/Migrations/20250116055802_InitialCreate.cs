using System;
using System.Collections.Generic;
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
                    start_block = table.Column<long>(type: "bigint", nullable: false),
                    end_block = table.Column<long>(type: "bigint", nullable: false),
                    interval = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    agent_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name_with_hash = table.Column<string>(type: "text", nullable: false),
                    portrait_id = table.Column<int>(type: "integer", nullable: false),
                    cp = table.Column<long>(type: "bigint", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.avatar_address);
                });

            migrationBuilder.CreateTable(
                name: "rounds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    start_block = table.Column<long>(type: "bigint", nullable: false),
                    end_block = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rounds", x => x.id);
                    table.ForeignKey(
                        name: "fk_rounds_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    initialized_score = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => new { x.avatar_address, x.season_id });
                    table.ForeignKey(
                        name: "fk_participants_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_participants_users_avatar_address",
                        column: x => x.avatar_address,
                        principalTable: "users",
                        principalColumn: "avatar_address",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "available_opponents",
                columns: table => new
                {
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    opponent_avatar_addresses = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_available_opponents", x => new { x.avatar_address, x.round_id });
                    table.ForeignKey(
                        name: "fk_available_opponents_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "available_opponents_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    update_source = table.Column<int>(type: "integer", nullable: false),
                    cost_paid = table.Column<int>(type: "integer", nullable: false),
                    tx_id = table.Column<string>(type: "text", nullable: true),
                    tx_status = table.Column<int>(type: "integer", nullable: true),
                    requested_avatar_addresses = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_available_opponents_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_available_opponents_requests_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "battle_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    attacker_avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    defender_avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    tx_id = table.Column<string>(type: "text", nullable: true),
                    tx_status = table.Column<int>(type: "integer", nullable: true),
                    is_victory = table.Column<bool>(type: "boolean", nullable: true),
                    participant_score_change = table.Column<int>(type: "integer", nullable: true),
                    opponent_score_change = table.Column<int>(type: "integer", nullable: true),
                    battle_block_index = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_battle_logs_participants_attacker_avatar_address_season_id",
                        columns: x => new { x.attacker_avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_logs_participants_defender_avatar_address_season_id",
                        columns: x => new { x.defender_avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_round_id",
                table: "available_opponents",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_requests_round_id",
                table: "available_opponents_requests",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_logs_attacker_avatar_address_season_id",
                table: "battle_logs",
                columns: new[] { "attacker_avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_battle_logs_defender_avatar_address_season_id",
                table: "battle_logs",
                columns: new[] { "defender_avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_participants_avatar_address",
                table: "participants",
                column: "avatar_address");

            migrationBuilder.CreateIndex(
                name: "ix_participants_season_id",
                table: "participants",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_rounds_season_id",
                table: "rounds",
                column: "season_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "available_opponents");

            migrationBuilder.DropTable(
                name: "available_opponents_requests");

            migrationBuilder.DropTable(
                name: "battle_logs");

            migrationBuilder.DropTable(
                name: "rounds");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
