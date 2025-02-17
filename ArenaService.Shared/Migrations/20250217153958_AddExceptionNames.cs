using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArenaService.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "exception_names",
                table: "refresh_ticket_purchase_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reviewed",
                table: "refresh_ticket_purchase_logs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exception_names",
                table: "battles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reviewed",
                table: "battles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exception_names",
                table: "battle_ticket_purchase_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reviewed",
                table: "battle_ticket_purchase_logs",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "exception_names",
                table: "refresh_ticket_purchase_logs");

            migrationBuilder.DropColumn(
                name: "reviewed",
                table: "refresh_ticket_purchase_logs");

            migrationBuilder.DropColumn(
                name: "exception_names",
                table: "battles");

            migrationBuilder.DropColumn(
                name: "reviewed",
                table: "battles");

            migrationBuilder.DropColumn(
                name: "exception_names",
                table: "battle_ticket_purchase_logs");

            migrationBuilder.DropColumn(
                name: "reviewed",
                table: "battle_ticket_purchase_logs");
        }
    }
}
