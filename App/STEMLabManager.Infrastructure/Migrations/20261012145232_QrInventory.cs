using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STEMLabManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QrInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryUnitCount",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventorySessionId = table.Column<long>(type: "INTEGER", nullable: false),
                    UnitId = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpectedStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Present = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryUnitCount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryUnitCount_Inventory_InventorySessionId",
                        column: x => x.InventorySessionId,
                        principalTable: "Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryUnitCount_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUnitCount_InventorySessionId_UnitId",
                table: "InventoryUnitCount",
                columns: new[] { "InventorySessionId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUnitCount_UnitId",
                table: "InventoryUnitCount",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryUnitCount");
        }
    }
}
