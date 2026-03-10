using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCSN.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomData",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "DeviceTokens",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Roles",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CustomData", table: "Users");

            migrationBuilder.DropColumn(name: "DeviceTokens", table: "Users");

            migrationBuilder.DropColumn(name: "Metadata", table: "Users");

            migrationBuilder.DropColumn(name: "Permissions", table: "Users");

            migrationBuilder.DropColumn(name: "Roles", table: "Users");
        }
    }
}
