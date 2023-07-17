using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MyDomainPasswordChange.Data.Sqlite.Migrations;

public partial class AddedBlackList : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
            name: "BlacklistedIps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AddedInBlacklist = table.Column<DateTime>(type: "TEXT", nullable: false),
                IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                Reason = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_BlacklistedIps", x => x.Id));

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
            name: "BlacklistedIps");
}
