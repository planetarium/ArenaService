using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prize_detail_url",
                table: "seasons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "lose_count",
                table: "battle_ticket_statuses_per_round",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "win_count",
                table: "battle_ticket_statuses_per_round",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prize_detail_url",
                table: "seasons");

            migrationBuilder.DropColumn(
                name: "lose_count",
                table: "battle_ticket_statuses_per_round");

            migrationBuilder.DropColumn(
                name: "win_count",
                table: "battle_ticket_statuses_per_round");
        }
    }
}
