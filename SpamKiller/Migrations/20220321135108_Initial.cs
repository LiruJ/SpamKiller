using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SpamKiller.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScamReporters",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BanCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastBanTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUnbanTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScamReporters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsWhitelisted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LogChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ReporterRoleId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BannedUsers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BannedUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BanReason = table.Column<int>(type: "INTEGER", nullable: false),
                    ReporterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BanDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BannedUsers_ScamReporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "ScamReporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreviousBannedUsers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BannedUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BanReason = table.Column<int>(type: "INTEGER", nullable: false),
                    UnbanReason = table.Column<string>(type: "TEXT", nullable: true),
                    BanningReporterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UnbanningReporterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BanDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UnbanDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreviousBannedUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreviousBannedUsers_ScamReporters_BanningReporterId",
                        column: x => x.BanningReporterId,
                        principalTable: "ScamReporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreviousBannedUsers_ScamReporters_UnbanningReporterId",
                        column: x => x.UnbanningReporterId,
                        principalTable: "ScamReporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannedUsers_ReporterId",
                table: "BannedUsers",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_PreviousBannedUsers_BanningReporterId",
                table: "PreviousBannedUsers",
                column: "BanningReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_PreviousBannedUsers_UnbanningReporterId",
                table: "PreviousBannedUsers",
                column: "UnbanningReporterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannedUsers");

            migrationBuilder.DropTable(
                name: "PreviousBannedUsers");

            migrationBuilder.DropTable(
                name: "ServerSettings");

            migrationBuilder.DropTable(
                name: "ScamReporters");
        }
    }
}
