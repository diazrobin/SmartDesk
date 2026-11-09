using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketPagingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tickets_CreatedBy",
                table: "tickets");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_CreatedAt",
                table: "tickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_CreatedBy_CreatedAt",
                table: "tickets",
                columns: new[] { "CreatedBy", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tickets_CreatedAt",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_CreatedBy_CreatedAt",
                table: "tickets");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_CreatedBy",
                table: "tickets",
                column: "CreatedBy");
        }
    }
}
