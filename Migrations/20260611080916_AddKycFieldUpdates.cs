using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistrationFormProject.Migrations
{
    /// <inheritdoc />
    public partial class AddKycFieldUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProfileVerified",
                table: "UserMasters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsReupload",
                table: "UserDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReuploadReason",
                table: "UserDocuments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProfileVerified",
                table: "UserMasters");

            migrationBuilder.DropColumn(
                name: "NeedsReupload",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "ReuploadReason",
                table: "UserDocuments");
        }
    }
}
