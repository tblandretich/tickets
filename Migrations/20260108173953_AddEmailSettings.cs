using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketsAndretich.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUsername = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpUseSsl = table.Column<bool>(type: "bit", nullable: false),
                    OAuth2ClientId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OAuth2ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OAuth2RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OAuth2AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OAuth2TokenExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsConfigured = table.Column<bool>(type: "bit", nullable: false),
                    LastSuccessfulTest = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSettings");
        }
    }
}
