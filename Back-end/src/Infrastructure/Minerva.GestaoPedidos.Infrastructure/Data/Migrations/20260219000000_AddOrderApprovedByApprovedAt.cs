using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minerva.GestaoPedidos.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderApprovedByApprovedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Orders");
        }
    }
}
