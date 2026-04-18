using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XcaNet.Storage.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RevocationAndCrlWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "CertificateRevocationLists",
                newName: "ThisUpdateUtc");

            migrationBuilder.RenameColumn(
                name: "AuthorityId",
                table: "CertificateRevocationLists",
                newName: "IssuerCertificateId");

            migrationBuilder.AddColumn<int>(
                name: "RevocationReason",
                table: "Certificates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "Certificates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CrlNumber",
                table: "CertificateRevocationLists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte[]>(
                name: "DerData",
                table: "CertificateRevocationLists",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "IssuerDisplayName",
                table: "CertificateRevocationLists",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "AuditEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AuditEvents",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CertificateRevocationListEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CertificateRevocationListId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRevocationListEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateRevocationListEntries_CertificateRevocationLists_CertificateRevocationListId",
                        column: x => x.CertificateRevocationListId,
                        principalTable: "CertificateRevocationLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_RevokedAtUtc",
                table: "Certificates",
                column: "RevokedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityId",
                table: "AuditEvents",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRevocationListEntries_CertificateRevocationListId",
                table: "CertificateRevocationListEntries",
                column: "CertificateRevocationListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateRevocationListEntries");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_RevokedAtUtc",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_EntityId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "CrlNumber",
                table: "CertificateRevocationLists");

            migrationBuilder.DropColumn(
                name: "DerData",
                table: "CertificateRevocationLists");

            migrationBuilder.DropColumn(
                name: "IssuerDisplayName",
                table: "CertificateRevocationLists");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AuditEvents");

            migrationBuilder.RenameColumn(
                name: "ThisUpdateUtc",
                table: "CertificateRevocationLists",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "IssuerCertificateId",
                table: "CertificateRevocationLists",
                newName: "AuthorityId");
        }
    }
}
