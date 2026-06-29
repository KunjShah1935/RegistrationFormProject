using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistrationFormProject.Migrations
{
    /// <inheritdoc />
    public partial class UserSecurityFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "UserMasters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "UserMasters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "UserMasters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "VerifiedDate",
                table: "UserDocuments",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMasters_EmailID",
                table: "UserMasters",
                column: "EmailID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMasters_MobileNo",
                table: "UserMasters",
                column: "MobileNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMasters_UserName",
                table: "UserMasters",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserMasters_EmailID",
                table: "UserMasters");

            migrationBuilder.DropIndex(
                name: "IX_UserMasters_MobileNo",
                table: "UserMasters");

            migrationBuilder.DropIndex(
                name: "IX_UserMasters_UserName",
                table: "UserMasters");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "UserMasters");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "UserMasters");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "UserMasters");

            migrationBuilder.AlterColumn<DateTime>(
                name: "VerifiedDate",
                table: "UserDocuments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }
    }
}
