using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEMLabManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UtcAndBiometricIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Consents_BorrowerId",
                table: "Consents");

            migrationBuilder.CreateIndex(
                name: "IX_Faces_ModelVersion",
                table: "Faces",
                column: "ModelVersion");

            migrationBuilder.CreateIndex(
                name: "IX_Consents_BorrowerId_RevokedUtc",
                table: "Consents",
                columns: new[] { "BorrowerId", "RevokedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faces_ModelVersion",
                table: "Faces");

            migrationBuilder.DropIndex(
                name: "IX_Consents_BorrowerId_RevokedUtc",
                table: "Consents");

            migrationBuilder.CreateIndex(
                name: "IX_Consents_BorrowerId",
                table: "Consents",
                column: "BorrowerId");
        }
    }
}
