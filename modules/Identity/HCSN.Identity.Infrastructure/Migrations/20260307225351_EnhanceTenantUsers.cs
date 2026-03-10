using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCSN.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceTenantUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_DeletedAt", table: "Users");

            migrationBuilder.DropIndex(name: "IX_Users_IsActive", table: "Users");

            migrationBuilder.DropIndex(name: "IX_Users_PhoneNumber", table: "Users");

            migrationBuilder.DropColumn(name: "CustomRegistrationUrl", table: "Tenants");

            migrationBuilder.DropColumn(name: "RequiresApproval", table: "Tenants");

            migrationBuilder.RenameColumn(name: "CustomData", table: "Users", newName: "Timezone");

            migrationBuilder.RenameColumn(
                name: "RequiredFields",
                table: "Tenants",
                newName: "SecurityPolicy"
            );

            migrationBuilder.RenameColumn(
                name: "RegistrationSettings",
                table: "Tenants",
                newName: "Metadata"
            );

            migrationBuilder.RenameColumn(name: "MaxUsers", table: "Tenants", newName: "Type");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20
            );

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "AccessibleSystems",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValueSql: "'[]'"
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotifications",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationAcceptedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationSentAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "InvitedBy",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "KnownDevices",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]"
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "LastLoginIp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "LastLoginUserAgent",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "LoginAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneConfirmedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PreferredTheme",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "PrivacyAccepted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyAcceptedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "PushNotifications",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantAdminAssignedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "TermsAccepted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorRecoveryCodes",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]"
            );

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Billing",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Branding",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "CustomDomain",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "DeploymentModel",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActiveAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Limits",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Tenants",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TenantModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleCode = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    ModuleName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EnabledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantModules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "IX_Users_Status", table: "Users", column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CustomDomain",
                table: "Tenants",
                column: "CustomDomain"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DeploymentModel",
                table: "Tenants",
                column: "DeploymentModel"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Status",
                table: "Tenants",
                column: "Status"
            );

            migrationBuilder.CreateIndex(name: "IX_Tenants_Type", table: "Tenants", column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId",
                table: "Invoices",
                column: "TenantId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TenantModules_TenantId",
                table: "TenantModules",
                column: "TenantId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Invoices");

            migrationBuilder.DropTable(name: "TenantModules");

            migrationBuilder.DropIndex(name: "IX_Users_Status", table: "Users");

            migrationBuilder.DropIndex(name: "IX_Tenants_CustomDomain", table: "Tenants");

            migrationBuilder.DropIndex(name: "IX_Tenants_DeploymentModel", table: "Tenants");

            migrationBuilder.DropIndex(name: "IX_Tenants_Status", table: "Tenants");

            migrationBuilder.DropIndex(name: "IX_Tenants_Type", table: "Tenants");

            migrationBuilder.DropColumn(name: "ApprovedAt", table: "Users");

            migrationBuilder.DropColumn(name: "ApprovedBy", table: "Users");

            migrationBuilder.DropColumn(name: "EmailConfirmedAt", table: "Users");

            migrationBuilder.DropColumn(name: "EmailNotifications", table: "Users");

            migrationBuilder.DropColumn(name: "InvitationAcceptedAt", table: "Users");

            migrationBuilder.DropColumn(name: "InvitationSentAt", table: "Users");

            migrationBuilder.DropColumn(name: "InvitationToken", table: "Users");

            migrationBuilder.DropColumn(name: "InvitedBy", table: "Users");

            migrationBuilder.DropColumn(name: "KnownDevices", table: "Users");

            migrationBuilder.DropColumn(name: "LastActivityAt", table: "Users");

            migrationBuilder.DropColumn(name: "LastLoginIp", table: "Users");

            migrationBuilder.DropColumn(name: "LastLoginUserAgent", table: "Users");

            migrationBuilder.DropColumn(name: "LastPasswordChangedAt", table: "Users");

            migrationBuilder.DropColumn(name: "LockoutEnd", table: "Users");

            migrationBuilder.DropColumn(name: "LoginAttempts", table: "Users");

            migrationBuilder.DropColumn(name: "PhoneConfirmedAt", table: "Users");

            migrationBuilder.DropColumn(name: "PreferredLanguage", table: "Users");

            migrationBuilder.DropColumn(name: "PreferredTheme", table: "Users");

            migrationBuilder.DropColumn(name: "PrivacyAccepted", table: "Users");

            migrationBuilder.DropColumn(name: "PrivacyAcceptedAt", table: "Users");

            migrationBuilder.DropColumn(name: "PushNotifications", table: "Users");

            migrationBuilder.DropColumn(name: "RejectedAt", table: "Users");

            migrationBuilder.DropColumn(name: "TenantAdminAssignedAt", table: "Users");

            migrationBuilder.DropColumn(name: "TermsAccepted", table: "Users");

            migrationBuilder.DropColumn(name: "TermsAcceptedAt", table: "Users");

            migrationBuilder.DropColumn(name: "TwoFactorRecoveryCodes", table: "Users");

            migrationBuilder.DropColumn(name: "UserType", table: "Users");

            migrationBuilder.DropColumn(name: "Billing", table: "Tenants");

            migrationBuilder.DropColumn(name: "Branding", table: "Tenants");

            migrationBuilder.DropColumn(name: "CustomDomain", table: "Tenants");

            migrationBuilder.DropColumn(name: "DeploymentModel", table: "Tenants");

            migrationBuilder.DropColumn(name: "LastActiveAt", table: "Tenants");

            migrationBuilder.DropColumn(name: "Limits", table: "Tenants");

            migrationBuilder.DropColumn(name: "Notes", table: "Tenants");

            migrationBuilder.DropColumn(name: "Status", table: "Tenants");

            migrationBuilder.RenameColumn(name: "Timezone", table: "Users", newName: "CustomData");

            migrationBuilder.RenameColumn(name: "Type", table: "Tenants", newName: "MaxUsers");

            migrationBuilder.RenameColumn(
                name: "SecurityPolicy",
                table: "Tenants",
                newName: "RequiredFields"
            );

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Tenants",
                newName: "RegistrationSettings"
            );

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)"
            );

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "AccessibleSystems",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "'[]'",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)"
            );

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)"
            );

            migrationBuilder.AddColumn<string>(
                name: "CustomRegistrationUrl",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedAt",
                table: "Users",
                column: "DeletedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber"
            );
        }
    }
}
