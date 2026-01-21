using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelegErfassungApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    IsAdminComment = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptComments_ReceiptComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "ReceiptComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptComments_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptComments_CreatedAt",
                table: "ReceiptComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptComments_ParentCommentId",
                table: "ReceiptComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptComments_ReceiptId",
                table: "ReceiptComments",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptComments_UserId",
                table: "ReceiptComments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptComments");
        }
    }
}
