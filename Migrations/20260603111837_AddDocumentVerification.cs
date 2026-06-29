using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistrationFormProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "UserDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VerifiedBy",
                table: "UserDocuments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedDate",
                table: "UserDocuments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedDate",
                table: "UserDocuments");
        }
    }
}
