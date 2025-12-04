using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Svipp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScooterEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scooters",
                columns: table => new
                {
                    ScooterId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Brand = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    MaxRangeKm = table.Column<int>(type: "integer", nullable: false),
                    BatteryLevelPercent = table.Column<int>(type: "integer", nullable: false),
                    CurrentLocationId = table.Column<int>(type: "integer", nullable: true),
                    DriverId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scooters", x => x.ScooterId);
                    table.ForeignKey(
                        name: "FK_Scooters_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scooters_Locations_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scooters_CurrentLocationId",
                table: "Scooters",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Scooters_DriverId",
                table: "Scooters",
                column: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scooters");
        }
    }
}
