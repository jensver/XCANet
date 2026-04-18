using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XcaNet.Storage.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TemplateSystemAndPolicyRefinement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDisabled",
                table: "Templates",
                newName: "ValidityDays");

            migrationBuilder.AddColumn<string>(
                name: "Curve",
                table: "Templates",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnhancedKeyUsages",
                table: "Templates",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IntendedUsage",
                table: "Templates",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCertificateAuthority",
                table: "Templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KeyAlgorithm",
                table: "Templates",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KeyUsages",
                table: "Templates",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PathLengthConstraint",
                table: "Templates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RsaKeySize",
                table: "Templates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureAlgorithm",
                table: "Templates",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectAlternativeNames",
                table: "Templates",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectDefault",
                table: "Templates",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_IntendedUsage",
                table: "Templates",
                column: "IntendedUsage");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_IsEnabled",
                table: "Templates",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_IsFavorite",
                table: "Templates",
                column: "IsFavorite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Templates_IntendedUsage",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_IsEnabled",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_IsFavorite",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Curve",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "EnhancedKeyUsages",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "IntendedUsage",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "IsCertificateAuthority",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "KeyAlgorithm",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "KeyUsages",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "PathLengthConstraint",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "RsaKeySize",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SignatureAlgorithm",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SubjectAlternativeNames",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SubjectDefault",
                table: "Templates");

            migrationBuilder.RenameColumn(
                name: "ValidityDays",
                table: "Templates",
                newName: "IsDisabled");
        }
    }
}
