using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XcaNet.Storage.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialStorageSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Authorities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CertificateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrivateKeyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentAuthorityId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    PemData = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRevocationLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AuthorityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NextUpdateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PemData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRevocationLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Issuer = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Sha1Thumbprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Sha256Thumbprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    NotBeforeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NotAfterUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevocationState = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuerCertificateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PrivateKeyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PemData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    KdfAlgorithm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    KdfIterations = table.Column<int>(type: "INTEGER", nullable: false),
                    KdfSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VerifierNonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VerifierCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VerifierTag = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptionAlgorithm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    KeyVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastOpenedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivateKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PublicKeyFingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    EncryptedPkcs8Ciphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptionNonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptionTag = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptionAlgorithm = table.Column<string>(type: "TEXT", nullable: false),
                    KeyVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDisabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateTags",
                columns: table => new
                {
                    CertificateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateTags", x => new { x.CertificateId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CertificateTags_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredUtc",
                table: "AuditEvents",
                column: "OccurredUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Issuer",
                table: "Certificates",
                column: "Issuer");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_IssuerCertificateId",
                table: "Certificates",
                column: "IssuerCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_NotAfterUtc",
                table: "Certificates",
                column: "NotAfterUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_NotBeforeUtc",
                table: "Certificates",
                column: "NotBeforeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_RevocationState",
                table: "Certificates",
                column: "RevocationState");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_SerialNumber",
                table: "Certificates",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Sha1Thumbprint",
                table: "Certificates",
                column: "Sha1Thumbprint");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Sha256Thumbprint",
                table: "Certificates",
                column: "Sha256Thumbprint");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Subject",
                table: "Certificates",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTags_TagId",
                table: "CertificateTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateKeys_PublicKeyFingerprint",
                table: "PrivateKeys",
                column: "PublicKeyFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Name",
                table: "Templates",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "Authorities");

            migrationBuilder.DropTable(
                name: "CertificateRequests");

            migrationBuilder.DropTable(
                name: "CertificateRevocationLists");

            migrationBuilder.DropTable(
                name: "CertificateTags");

            migrationBuilder.DropTable(
                name: "DatabaseProfiles");

            migrationBuilder.DropTable(
                name: "PrivateKeys");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
