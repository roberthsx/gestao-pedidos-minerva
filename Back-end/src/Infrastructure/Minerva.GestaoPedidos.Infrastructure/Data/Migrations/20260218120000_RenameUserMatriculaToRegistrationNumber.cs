using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Minerva.GestaoPedidos.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class RenameUserMatriculaToRegistrationNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Matricula",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Matricula",
                table: "Users",
                newName: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RegistrationNumber",
                table: "Users",
                column: "RegistrationNumber",
                unique: true,
                filter: "\"RegistrationNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_RegistrationNumber",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "Users",
                newName: "Matricula");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Matricula",
                table: "Users",
                column: "Matricula",
                unique: true,
                filter: "\"Matricula\" IS NOT NULL");
        }
    }
}
