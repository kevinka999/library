using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PublishDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Authors = table.Column<string[]>(type: "text[]", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.CheckConstraint("CK_Books_Authors_NotEmpty", "cardinality(\"Authors\") > 0");
                    table.CheckConstraint("CK_Books_ShortDescription_NotBlank", "length(btrim(\"ShortDescription\")) > 0");
                    table.CheckConstraint("CK_Books_Title_NotBlank", "length(btrim(\"Title\")) > 0");
                    table.CheckConstraint("CK_Books_Version_Positive", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "BookChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookId = table.Column<long>(type: "bigint", nullable: false),
                    ChangeSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedField = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OldValue = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    NewValue = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookChanges", x => x.Id);
                    table.CheckConstraint("CK_BookChanges_ChangedField", "\"ChangedField\" IN ('title', 'shortDescription', 'publishDate', 'authors')");
                    table.ForeignKey(
                        name: "FK_BookChanges_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookChanges_BookId_ChangedAt_ChangeSetId",
                table: "BookChanges",
                columns: new[] { "BookId", "ChangedAt", "ChangeSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_BookChanges_BookId_ChangedField_ChangedAt_ChangeSetId",
                table: "BookChanges",
                columns: new[] { "BookId", "ChangedField", "ChangedAt", "ChangeSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_BookChanges_BookId_ChangeSetId_ChangedField",
                table: "BookChanges",
                columns: new[] { "BookId", "ChangeSetId", "ChangedField" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookChanges");

            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
