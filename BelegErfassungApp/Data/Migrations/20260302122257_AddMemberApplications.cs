using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelegErfassungApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptComments_Receipts_ReceiptId",
                table: "ReceiptComments");

            migrationBuilder.CreateTable(
                name: "MemberApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProcessedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nachname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Vorname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Geburtsdatum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BerufTaetigkeit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strasse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PLZ = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Wohnort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Antragsdatum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnterschriftVorhanden = table.Column<bool>(type: "bit", nullable: false),
                    Kontoinhaber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Geldinstitut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IBAN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SEPADatum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SEPAUnterschriftVorhanden = table.Column<bool>(type: "bit", nullable: false),
                    OcrRawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcrProcessed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberApplications_AspNetUsers_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemberApplications_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_ProcessedByUserId",
                table: "MemberApplications",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberApplications_UploadedByUserId",
                table: "MemberApplications",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptComments_Receipts_ReceiptId",
                table: "ReceiptComments",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptComments_Receipts_ReceiptId",
                table: "ReceiptComments");

            migrationBuilder.DropTable(
                name: "MemberApplications");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptComments_Receipts_ReceiptId",
                table: "ReceiptComments",
                column: "ReceiptId",
                principalTable: "Receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
