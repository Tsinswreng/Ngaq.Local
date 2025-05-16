using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ngaq.Local.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kv",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastUpdatedBy = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Status = table.Column<long>(type: "INTEGER", nullable: false),
                    FKeyType = table.Column<long>(type: "INTEGER", nullable: false),
                    FKey_Str = table.Column<string>(type: "TEXT", nullable: true),
                    FKey_UInt128 = table.Column<byte[]>(type: "BLOB", nullable: true),
                    KType = table.Column<long>(type: "INTEGER", nullable: false),
                    KStr = table.Column<string>(type: "TEXT", nullable: true),
                    KI64 = table.Column<long>(type: "INTEGER", nullable: false),
                    KDescr = table.Column<string>(type: "TEXT", nullable: true),
                    VType = table.Column<long>(type: "INTEGER", nullable: false),
                    VDescr = table.Column<string>(type: "TEXT", nullable: true),
                    VStr = table.Column<string>(type: "TEXT", nullable: true),
                    VI64 = table.Column<long>(type: "INTEGER", nullable: false),
                    VF64 = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kv", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Learn",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastUpdatedBy = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Status = table.Column<long>(type: "INTEGER", nullable: false),
                    FKeyType = table.Column<long>(type: "INTEGER", nullable: false),
                    FKey_Str = table.Column<string>(type: "TEXT", nullable: true),
                    FKey_UInt128 = table.Column<byte[]>(type: "BLOB", nullable: true),
                    KType = table.Column<long>(type: "INTEGER", nullable: false),
                    KStr = table.Column<string>(type: "TEXT", nullable: true),
                    KI64 = table.Column<long>(type: "INTEGER", nullable: false),
                    KDescr = table.Column<string>(type: "TEXT", nullable: true),
                    VType = table.Column<long>(type: "INTEGER", nullable: false),
                    VDescr = table.Column<string>(type: "TEXT", nullable: true),
                    VStr = table.Column<string>(type: "TEXT", nullable: true),
                    VI64 = table.Column<long>(type: "INTEGER", nullable: false),
                    VF64 = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Learn", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Word",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Owner = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastUpdatedBy = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Status = table.Column<long>(type: "INTEGER", nullable: false),
                    WordFormId = table.Column<string>(type: "TEXT", nullable: false),
                    Lang = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Word", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kv_CreatedBy",
                table: "Kv",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Kv_FKey_UInt128",
                table: "Kv",
                column: "FKey_UInt128");

            migrationBuilder.CreateIndex(
                name: "IX_Kv_KI64",
                table: "Kv",
                column: "KI64");

            migrationBuilder.CreateIndex(
                name: "IX_Kv_KStr",
                table: "Kv",
                column: "KStr");

            migrationBuilder.CreateIndex(
                name: "IX_Learn_CreatedBy",
                table: "Learn",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Learn_FKey_UInt128",
                table: "Learn",
                column: "FKey_UInt128");

            migrationBuilder.CreateIndex(
                name: "IX_Learn_KI64",
                table: "Learn",
                column: "KI64");

            migrationBuilder.CreateIndex(
                name: "IX_Learn_KStr",
                table: "Learn",
                column: "KStr");

            migrationBuilder.CreateIndex(
                name: "IX_Word_CreatedBy",
                table: "Word",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Word_WordFormId",
                table: "Word",
                column: "WordFormId");

            migrationBuilder.CreateIndex(
                name: "IX_Word_WordFormId_Lang_Owner",
                table: "Word",
                columns: new[] { "WordFormId", "Lang", "Owner" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kv");

            migrationBuilder.DropTable(
                name: "Learn");

            migrationBuilder.DropTable(
                name: "Word");
        }
    }
}
