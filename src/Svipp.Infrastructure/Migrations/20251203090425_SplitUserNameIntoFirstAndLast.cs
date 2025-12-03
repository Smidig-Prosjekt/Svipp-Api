using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Svipp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitUserNameIntoFirstAndLast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op migration for current schema. We keep this migration in history so EF is happy,
            // but the actual schema now matches the latest model from the initial create migration.
            migrationBuilder.Sql(@"ALTER TABLE ""users"" DROP COLUMN IF EXISTS ""FullName"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: we don't need to revert anything for development purposes.
        }
    }
}
