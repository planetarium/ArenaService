﻿// <auto-generated />
using System;
using ArenaService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArenaService.Migrations
{
    [DbContext(typeof(ArenaDbContext))]
    [Migration("20241226151924_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ArenaService.Models.AvailableOpponent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsBattled")
                        .HasColumnType("boolean")
                        .HasColumnName("is_battled");

                    b.Property<int>("OpponentId")
                        .HasColumnType("integer")
                        .HasColumnName("opponent_id");

                    b.Property<int>("ParticipantId")
                        .HasColumnType("integer")
                        .HasColumnName("participant_id");

                    b.Property<long>("RefillBlockIndex")
                        .HasColumnType("bigint")
                        .HasColumnName("refill_block_index");

                    b.HasKey("Id")
                        .HasName("pk_available_opponents");

                    b.HasIndex("OpponentId")
                        .HasDatabaseName("ix_available_opponents_opponent_id");

                    b.HasIndex("ParticipantId")
                        .HasDatabaseName("ix_available_opponents_participant_id");

                    b.ToTable("available_opponents", (string)null);
                });

            modelBuilder.Entity("ArenaService.Models.BattleLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<long?>("BattleBlockIndex")
                        .HasColumnType("bigint")
                        .HasColumnName("battle_block_index");

                    b.Property<bool?>("IsVictory")
                        .HasColumnType("boolean")
                        .HasColumnName("is_victory");

                    b.Property<int>("OpponentId")
                        .HasColumnType("integer")
                        .HasColumnName("opponent_id");

                    b.Property<int?>("OpponentScoreChange")
                        .HasColumnType("integer")
                        .HasColumnName("opponent_score_change");

                    b.Property<int>("ParticipantId")
                        .HasColumnType("integer")
                        .HasColumnName("participant_id");

                    b.Property<int?>("ParticipantScoreChange")
                        .HasColumnType("integer")
                        .HasColumnName("participant_score_change");

                    b.Property<int>("SeasonId")
                        .HasColumnType("integer")
                        .HasColumnName("season_id");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("token");

                    b.HasKey("Id")
                        .HasName("pk_battle_logs");

                    b.HasIndex("OpponentId")
                        .HasDatabaseName("ix_battle_logs_opponent_id");

                    b.HasIndex("ParticipantId")
                        .HasDatabaseName("ix_battle_logs_participant_id");

                    b.HasIndex("SeasonId")
                        .HasDatabaseName("ix_battle_logs_season_id");

                    b.ToTable("battle_logs", (string)null);
                });

            modelBuilder.Entity("ArenaService.Models.LeaderboardEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ParticipantId")
                        .HasColumnType("integer")
                        .HasColumnName("participant_id");

                    b.Property<int>("Rank")
                        .HasColumnType("integer")
                        .HasColumnName("rank");

                    b.Property<int>("SeasonId")
                        .HasColumnType("integer")
                        .HasColumnName("season_id");

                    b.Property<int>("TotalScore")
                        .HasColumnType("integer")
                        .HasColumnName("total_score");

                    b.HasKey("Id")
                        .HasName("pk_leaderboard");

                    b.HasIndex("ParticipantId")
                        .HasDatabaseName("ix_leaderboard_participant_id");

                    b.HasIndex("SeasonId")
                        .HasDatabaseName("ix_leaderboard_season_id");

                    b.ToTable("leaderboard", (string)null);
                });

            modelBuilder.Entity("ArenaService.Models.Participant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AvatarAddress")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("avatar_address");

                    b.Property<string>("NameWithHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name_with_hash");

                    b.Property<int>("PortraitId")
                        .HasColumnType("integer")
                        .HasColumnName("portrait_id");

                    b.Property<int>("SeasonId")
                        .HasColumnType("integer")
                        .HasColumnName("season_id");

                    b.HasKey("Id")
                        .HasName("pk_participants");

                    b.HasIndex("AvatarAddress")
                        .HasDatabaseName("ix_participants_avatar_address");

                    b.HasIndex("SeasonId")
                        .HasDatabaseName("ix_participants_season_id");

                    b.ToTable("participants", (string)null);
                });

            modelBuilder.Entity("ArenaService.Models.Season", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<long>("EndBlockIndex")
                        .HasColumnType("bigint")
                        .HasColumnName("end_block_index");

                    b.Property<bool>("IsActivated")
                        .HasColumnType("boolean")
                        .HasColumnName("is_activated");

                    b.Property<long>("StartBlockIndex")
                        .HasColumnType("bigint")
                        .HasColumnName("start_block_index");

                    b.Property<int>("TicketRefillInterval")
                        .HasColumnType("integer")
                        .HasColumnName("ticket_refill_interval");

                    b.HasKey("Id")
                        .HasName("pk_seasons");

                    b.ToTable("seasons", (string)null);
                });

            modelBuilder.Entity("ArenaService.Models.AvailableOpponent", b =>
                {
                    b.HasOne("ArenaService.Models.Participant", "Opponent")
                        .WithMany()
                        .HasForeignKey("OpponentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_available_opponents_participants_opponent_id");

                    b.HasOne("ArenaService.Models.Participant", "Participant")
                        .WithMany()
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_available_opponents_participants_participant_id");

                    b.Navigation("Opponent");

                    b.Navigation("Participant");
                });

            modelBuilder.Entity("ArenaService.Models.BattleLog", b =>
                {
                    b.HasOne("ArenaService.Models.Participant", "Opponent")
                        .WithMany()
                        .HasForeignKey("OpponentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_battle_logs_participants_opponent_id");

                    b.HasOne("ArenaService.Models.Participant", "Participant")
                        .WithMany("BattleLogs")
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_battle_logs_participants_participant_id");

                    b.HasOne("ArenaService.Models.Season", "Season")
                        .WithMany("BattleLogs")
                        .HasForeignKey("SeasonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_battle_logs_seasons_season_id");

                    b.Navigation("Opponent");

                    b.Navigation("Participant");

                    b.Navigation("Season");
                });

            modelBuilder.Entity("ArenaService.Models.LeaderboardEntry", b =>
                {
                    b.HasOne("ArenaService.Models.Participant", "Participant")
                        .WithMany("Leaderboard")
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_leaderboard_participants_participant_id");

                    b.HasOne("ArenaService.Models.Season", "Season")
                        .WithMany("Leaderboard")
                        .HasForeignKey("SeasonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_leaderboard_seasons_season_id");

                    b.Navigation("Participant");

                    b.Navigation("Season");
                });

            modelBuilder.Entity("ArenaService.Models.Participant", b =>
                {
                    b.HasOne("ArenaService.Models.Season", "Season")
                        .WithMany("Participants")
                        .HasForeignKey("SeasonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_participants_seasons_season_id");

                    b.Navigation("Season");
                });

            modelBuilder.Entity("ArenaService.Models.Participant", b =>
                {
                    b.Navigation("BattleLogs");

                    b.Navigation("Leaderboard");
                });

            modelBuilder.Entity("ArenaService.Models.Season", b =>
                {
                    b.Navigation("BattleLogs");

                    b.Navigation("Leaderboard");

                    b.Navigation("Participants");
                });
#pragma warning restore 612, 618
        }
    }
}