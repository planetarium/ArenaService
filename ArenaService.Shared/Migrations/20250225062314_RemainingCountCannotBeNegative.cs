using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Shared.Migrations
{
    /// <inheritdoc />
    public partial class RemainingCountCannotBeNegative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshTicketStatusPerRound_RemainingCount",
                table: "refresh_ticket_statuses_per_round",
                sql: "remaining_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BattleTicketStatusPerRound_RemainingCount",
                table: "battle_ticket_statuses_per_round",
                sql: "remaining_count >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshTicketStatusPerRound_RemainingCount",
                table: "refresh_ticket_statuses_per_round");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BattleTicketStatusPerRound_RemainingCount",
                table: "battle_ticket_statuses_per_round");
        }
    }
}
