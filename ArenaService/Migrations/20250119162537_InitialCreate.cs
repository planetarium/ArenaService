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
                name: "refresh_price_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_price_policies", x => x.id);
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
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.avatar_address);
                });

            migrationBuilder.CreateTable(
                name: "refresh_price_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    policy_id = table.Column<int>(type: "integer", nullable: false),
                    refresh_order = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_price_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_price_details_refresh_price_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "refresh_price_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_block = table.Column<long>(type: "bigint", nullable: false),
                    end_block = table.Column<long>(type: "bigint", nullable: false),
                    arena_type = table.Column<int>(type: "integer", nullable: false),
                    round_interval = table.Column<int>(type: "integer", nullable: false),
                    price_policy_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                    table.ForeignKey(
                        name: "fk_seasons_refresh_price_policies_price_policy_id",
                        column: x => x.price_policy_id,
                        principalTable: "refresh_price_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
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
                name: "refresh_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    refresh_price_detail_id = table.Column<int>(type: "integer", nullable: false),
                    is_cost_paid = table.Column<bool>(type: "boolean", nullable: false),
                    refresh_status = table.Column<int>(type: "integer", nullable: false),
                    tx_id = table.Column<string>(type: "text", nullable: true),
                    tx_status = table.Column<int>(type: "integer", nullable: true),
                    specified_avatar_addresses = table.Column<List<string>>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_requests_refresh_price_details_refresh_price_detail",
                        column: x => x.refresh_price_detail_id,
                        principalTable: "refresh_price_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refresh_requests_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refresh_requests_seasons_season_id",
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
                    last_refresh_request_id = table.Column<int>(type: "integer", nullable: true),
                    initialized_score = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => new { x.avatar_address, x.season_id });
                    table.ForeignKey(
                        name: "fk_participants_refresh_requests_last_refresh_request_id",
                        column: x => x.last_refresh_request_id,
                        principalTable: "refresh_requests",
                        principalColumn: "id");
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
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "available_opponents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    opponent_avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    refresh_request_id = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    battle_log_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_available_opponents", x => x.id);
                    table.ForeignKey(
                        name: "fk_available_opponents_battle_logs_battle_log_id",
                        column: x => x.battle_log_id,
                        principalTable: "battle_logs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_available_opponents_participants_avatar_address_season_id",
                        columns: x => new { x.avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_available_opponents_participants_opponent_avatar_address_se",
                        columns: x => new { x.opponent_avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_available_opponents_refresh_requests_refresh_request_id",
                        column: x => x.refresh_request_id,
                        principalTable: "refresh_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_available_opponents_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_available_opponents_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_avatar_address_season_id",
                table: "available_opponents",
                columns: new[] { "avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_battle_log_id",
                table: "available_opponents",
                column: "battle_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_opponent_avatar_address_season_id",
                table: "available_opponents",
                columns: new[] { "opponent_avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_refresh_request_id",
                table: "available_opponents",
                column: "refresh_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_round_id",
                table: "available_opponents",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_season_id",
                table: "available_opponents",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_logs_attacker_avatar_address_season_id",
                table: "battle_logs",
                columns: new[] { "attacker_avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_battle_logs_defender_avatar_address_season_id",
                table: "battle_logs",
                columns: new[] { "defender_avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_participants_last_refresh_request_id",
                table: "participants",
                column: "last_refresh_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_season_id",
                table: "participants",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_price_details_policy_id",
                table: "refresh_price_details",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_requests_refresh_price_detail_id",
                table: "refresh_requests",
                column: "refresh_price_detail_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_requests_round_id",
                table: "refresh_requests",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_requests_season_id",
                table: "refresh_requests",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_rounds_season_id",
                table: "rounds",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_price_policy_id",
                table: "seasons",
                column: "price_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_agent_address",
                table: "users",
                column: "agent_address",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "available_opponents");

            migrationBuilder.DropTable(
                name: "battle_logs");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "refresh_requests");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "refresh_price_details");

            migrationBuilder.DropTable(
                name: "rounds");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "refresh_price_policies");
        }
    }
}
