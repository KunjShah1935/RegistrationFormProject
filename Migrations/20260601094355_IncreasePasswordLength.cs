using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistrationFormProject.Migrations
{
    /// <inheritdoc />
    public partial class IncreasePasswordLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "UserMasters",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_UserMasters_RoleId",
                table: "UserMasters",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMasters_RoleMasters_RoleId",
                table: "UserMasters",
                column: "RoleId",
                principalTable: "RoleMasters",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserMasters_RoleMasters_RoleId",
                table: "UserMasters");

            migrationBuilder.DropIndex(
                name: "IX_UserMasters_RoleId",
                table: "UserMasters");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "UserMasters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
