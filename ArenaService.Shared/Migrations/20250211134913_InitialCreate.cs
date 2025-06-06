﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArenaService.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "battle_ticket_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    max_purchasable_tickets_per_season = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    default_tickets_per_round = table.Column<int>(type: "integer", nullable: false),
                    max_purchasable_tickets_per_round = table.Column<int>(type: "integer", nullable: false),
                    purchase_prices = table.Column<List<decimal>>(type: "decimal[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_ticket_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "battle_ticket_purchase_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    purchase_status = table.Column<int>(type: "integer", nullable: false),
                    purchase_count = table.Column<int>(type: "integer", nullable: false),
                    tx_id = table.Column<string>(type: "text", nullable: false),
                    tx_status = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_ticket_purchase_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_ticket_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    default_tickets_per_round = table.Column<int>(type: "integer", nullable: false),
                    max_purchasable_tickets_per_round = table.Column<int>(type: "integer", nullable: false),
                    purchase_prices = table.Column<List<decimal>>(type: "decimal[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_ticket_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_ticket_purchase_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    purchase_status = table.Column<int>(type: "integer", nullable: false),
                    purchase_count = table.Column<int>(type: "integer", nullable: false),
                    tx_id = table.Column<string>(type: "text", nullable: false),
                    tx_status = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_ticket_purchase_logs", x => x.id);
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
                    clan_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.avatar_address);
                    table.ForeignKey(
                        name: "fk_users_clans_clan_id",
                        column: x => x.clan_id,
                        principalTable: "clans",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_group_id = table.Column<int>(type: "integer", nullable: false),
                    start_block = table.Column<long>(type: "bigint", nullable: false),
                    end_block = table.Column<long>(type: "bigint", nullable: false),
                    arena_type = table.Column<int>(type: "integer", nullable: false),
                    round_interval = table.Column<int>(type: "integer", nullable: false),
                    required_medal_count = table.Column<int>(type: "integer", nullable: false),
                    total_prize = table.Column<int>(type: "integer", nullable: false),
                    prize_detail_url = table.Column<string>(type: "text", nullable: false),
                    battle_ticket_policy_id = table.Column<int>(type: "integer", nullable: false),
                    refresh_ticket_policy_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                    table.ForeignKey(
                        name: "fk_seasons_battle_ticket_policies_battle_ticket_policy_id",
                        column: x => x.battle_ticket_policy_id,
                        principalTable: "battle_ticket_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_seasons_refresh_ticket_policies_refresh_ticket_policy_id",
                        column: x => x.refresh_ticket_policy_id,
                        principalTable: "refresh_ticket_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medals",
                columns: table => new
                {
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    medal_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medals", x => new { x.avatar_address, x.season_id });
                    table.ForeignKey(
                        name: "fk_medals_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_medals_users_avatar_address",
                        column: x => x.avatar_address,
                        principalTable: "users",
                        principalColumn: "avatar_address",
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
                    total_win = table.Column<int>(type: "integer", nullable: false),
                    total_lose = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
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
                name: "battle_ticket_statuses_per_season",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    battle_ticket_policy_id = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    used_count = table.Column<int>(type: "integer", nullable: false),
                    purchase_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_ticket_statuses_per_season", x => x.id);
                    table.ForeignKey(
                        name: "fk_battle_ticket_statuses_per_season_battle_ticket_policies_ba",
                        column: x => x.battle_ticket_policy_id,
                        principalTable: "battle_ticket_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_ticket_statuses_per_season_participants_avatar_addre",
                        columns: x => new { x.avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_ticket_statuses_per_season_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "battle_ticket_statuses_per_round",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    battle_ticket_policy_id = table.Column<int>(type: "integer", nullable: false),
                    remaining_count = table.Column<int>(type: "integer", nullable: false),
                    win_count = table.Column<int>(type: "integer", nullable: false),
                    lose_count = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    used_count = table.Column<int>(type: "integer", nullable: false),
                    purchase_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_ticket_statuses_per_round", x => x.id);
                    table.ForeignKey(
                        name: "fk_battle_ticket_statuses_per_round_battle_ticket_policies_bat",
                        column: x => x.battle_ticket_policy_id,
                        principalTable: "battle_ticket_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_ticket_statuses_per_round_participants_avatar_addres",
                        columns: x => new { x.avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_ticket_statuses_per_round_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_ticket_statuses_per_round_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "refresh_ticket_statuses_per_round",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    refresh_ticket_policy_id = table.Column<int>(type: "integer", nullable: false),
                    remaining_count = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    used_count = table.Column<int>(type: "integer", nullable: false),
                    purchase_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_ticket_statuses_per_round", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_ticket_statuses_per_round_participants_avatar_addre",
                        columns: x => new { x.avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refresh_ticket_statuses_per_round_refresh_ticket_policies_r",
                        column: x => x.refresh_ticket_policy_id,
                        principalTable: "refresh_ticket_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refresh_ticket_statuses_per_round_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_refresh_ticket_statuses_per_round_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "battle_ticket_usage_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    battle_ticket_status_per_round_id = table.Column<int>(type: "integer", nullable: false),
                    battle_ticket_status_per_season_id = table.Column<int>(type: "integer", nullable: false),
                    battle_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battle_ticket_usage_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_battle_ticket_usage_logs_battle_ticket_statuses_per_round_b",
                        column: x => x.battle_ticket_status_per_round_id,
                        principalTable: "battle_ticket_statuses_per_round",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battle_ticket_usage_logs_battle_ticket_statuses_per_season_",
                        column: x => x.battle_ticket_status_per_season_id,
                        principalTable: "battle_ticket_statuses_per_season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_ticket_usage_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    refresh_ticket_status_per_round_id = table.Column<int>(type: "integer", nullable: false),
                    specified_opponent_ids = table.Column<List<int>>(type: "integer[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_ticket_usage_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_ticket_usage_logs_refresh_ticket_statuses_per_round",
                        column: x => x.refresh_ticket_status_per_round_id,
                        principalTable: "refresh_ticket_statuses_per_round",
                        principalColumn: "id",
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
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    success_battle_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_available_opponents", x => x.id);
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

            migrationBuilder.CreateTable(
                name: "battles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatar_address = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    available_opponent_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    battle_status = table.Column<int>(type: "integer", nullable: false),
                    tx_id = table.Column<string>(type: "text", nullable: true),
                    tx_status = table.Column<int>(type: "integer", nullable: true),
                    is_victory = table.Column<bool>(type: "boolean", nullable: true),
                    my_score_change = table.Column<int>(type: "integer", nullable: true),
                    opponent_score_change = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_battles", x => x.id);
                    table.ForeignKey(
                        name: "fk_battles_available_opponents_available_opponent_id",
                        column: x => x.available_opponent_id,
                        principalTable: "available_opponents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battles_participants_avatar_address_season_id",
                        columns: x => new { x.avatar_address, x.season_id },
                        principalTable: "participants",
                        principalColumns: new[] { "avatar_address", "season_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_battles_seasons_season_id",
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
                name: "ix_available_opponents_opponent_avatar_address_season_id",
                table: "available_opponents",
                columns: new[] { "opponent_avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_round_id",
                table: "available_opponents",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_season_id",
                table: "available_opponents",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_available_opponents_success_battle_id",
                table: "available_opponents",
                column: "success_battle_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_statuses_per_round_avatar_address_season_id_r",
                table: "battle_ticket_statuses_per_round",
                columns: new[] { "avatar_address", "season_id", "round_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_statuses_per_round_battle_ticket_policy_id",
                table: "battle_ticket_statuses_per_round",
                column: "battle_ticket_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_statuses_per_round_round_id",
                table: "battle_ticket_statuses_per_round",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_statuses_per_round_season_id",
                table: "battle_ticket_statuses_per_round",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_statuses_per_season_avatar_address_season_id",
                table: "battle_ticket_statuses_per_season",
                columns: new[] { "avatar_address", "season_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_statuses_per_season_battle_ticket_policy_id",
                table: "battle_ticket_statuses_per_season",
                column: "battle_ticket_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_statuses_per_season_season_id",
                table: "battle_ticket_statuses_per_season",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_usage_logs_battle_ticket_status_per_round_id",
                table: "battle_ticket_usage_logs",
                column: "battle_ticket_status_per_round_id");

            migrationBuilder.CreateIndex(
                name: "ix_battle_ticket_usage_logs_battle_ticket_status_per_season_id",
                table: "battle_ticket_usage_logs",
                column: "battle_ticket_status_per_season_id");

            migrationBuilder.CreateIndex(
                name: "ix_battles_available_opponent_id",
                table: "battles",
                column: "available_opponent_id");

            migrationBuilder.CreateIndex(
                name: "ix_battles_avatar_address_season_id",
                table: "battles",
                columns: new[] { "avatar_address", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_battles_season_id",
                table: "battles",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_clan_ranking_snapshots_round_id",
                table: "clan_ranking_snapshots",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_clan_ranking_snapshots_season_id_round_id",
                table: "clan_ranking_snapshots",
                columns: new[] { "season_id", "round_id" });

            migrationBuilder.CreateIndex(
                name: "ix_medals_medal_count_season_id",
                table: "medals",
                columns: new[] { "medal_count", "season_id" });

            migrationBuilder.CreateIndex(
                name: "ix_medals_season_id",
                table: "medals",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_participants_season_id",
                table: "participants",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_ranking_snapshots_round_id",
                table: "ranking_snapshots",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_ranking_snapshots_season_id_round_id",
                table: "ranking_snapshots",
                columns: new[] { "season_id", "round_id" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_ticket_statuses_per_round_avatar_address_season_id_",
                table: "refresh_ticket_statuses_per_round",
                columns: new[] { "avatar_address", "season_id", "round_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_ticket_statuses_per_round_refresh_ticket_policy_id",
                table: "refresh_ticket_statuses_per_round",
                column: "refresh_ticket_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_ticket_statuses_per_round_round_id",
                table: "refresh_ticket_statuses_per_round",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_ticket_statuses_per_round_season_id",
                table: "refresh_ticket_statuses_per_round",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_ticket_usage_logs_refresh_ticket_status_per_round_id",
                table: "refresh_ticket_usage_logs",
                column: "refresh_ticket_status_per_round_id");

            migrationBuilder.CreateIndex(
                name: "ix_rounds_season_id",
                table: "rounds",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_battle_ticket_policy_id",
                table: "seasons",
                column: "battle_ticket_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_refresh_ticket_policy_id",
                table: "seasons",
                column: "refresh_ticket_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_start_block",
                table: "seasons",
                column: "start_block");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_start_block_end_block",
                table: "seasons",
                columns: new[] { "start_block", "end_block" });

            migrationBuilder.CreateIndex(
                name: "ix_users_clan_id",
                table: "users",
                column: "clan_id");

            migrationBuilder.AddForeignKey(
                name: "fk_available_opponents_battles_success_battle_id",
                table: "available_opponents",
                column: "success_battle_id",
                principalTable: "battles",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_available_opponents_battles_success_battle_id",
                table: "available_opponents");

            migrationBuilder.DropTable(
                name: "battle_ticket_purchase_logs");

            migrationBuilder.DropTable(
                name: "battle_ticket_usage_logs");

            migrationBuilder.DropTable(
                name: "clan_ranking_snapshots");

            migrationBuilder.DropTable(
                name: "medals");

            migrationBuilder.DropTable(
                name: "ranking_snapshots");

            migrationBuilder.DropTable(
                name: "refresh_ticket_purchase_logs");

            migrationBuilder.DropTable(
                name: "refresh_ticket_usage_logs");

            migrationBuilder.DropTable(
                name: "battle_ticket_statuses_per_round");

            migrationBuilder.DropTable(
                name: "battle_ticket_statuses_per_season");

            migrationBuilder.DropTable(
                name: "refresh_ticket_statuses_per_round");

            migrationBuilder.DropTable(
                name: "battles");

            migrationBuilder.DropTable(
                name: "available_opponents");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "rounds");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "clans");

            migrationBuilder.DropTable(
                name: "battle_ticket_policies");

            migrationBuilder.DropTable(
                name: "refresh_ticket_policies");
        }
    }
}
