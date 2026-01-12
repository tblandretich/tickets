using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketsAndretich.Web.Migrations
{
    /// <inheritdoc />
    public partial class TicketTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaInicioTratamiento",
                table: "Tickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificacionCancelacion",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificacionReasignacion",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TiempoEstimadoHoras",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimaAccionPorId",
                table: "Tickets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimaAccionPorUserId",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UltimaAccionPorId",
                table: "Tickets",
                column: "UltimaAccionPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_UltimaAccionPorId",
                table: "Tickets",
                column: "UltimaAccionPorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_UltimaAccionPorId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_UltimaAccionPorId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FechaInicioTratamiento",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "JustificacionCancelacion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "JustificacionReasignacion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TiempoEstimadoHoras",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UltimaAccionPorId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UltimaAccionPorUserId",
                table: "Tickets");
        }
    }
}
