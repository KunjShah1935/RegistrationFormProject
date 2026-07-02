using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistrationFormProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudinaryFieldsToUserDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicId",
                table: "UserDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudinaryUrl",
                table: "UserDocuments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudinaryPublicId",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "CloudinaryUrl",
                table: "UserDocuments");
        }
    }
}
