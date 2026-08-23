using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260823210000_AddLoperId")]
public sealed class AddLoperId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence<long>(
            name: "LoperIdSequence",
            startValue: 1L);

        migrationBuilder.Sql(
            """
            INSERT INTO [PersonIdentifiers]
                ([Id], [PersonId], [IdentifierType], [Value], [IsProtected], [CreatedUtc])
            SELECT
                NEWID(),
                p.[Id],
                5,
                CONCAT(
                    'LOPER-',
                    RIGHT(
                        '000000' + CONVERT(varchar(20),
                            NEXT VALUE FOR [LoperIdSequence]
                                OVER (ORDER BY p.[CreatedUtc], p.[Id])),
                        6)),
                1,
                SYSUTCDATETIME()
            FROM [People] p
            WHERE NOT EXISTS (
                SELECT 1
                FROM [PersonIdentifiers] i
                WHERE i.[PersonId] = p.[Id]
                  AND i.[IdentifierType] = 5);
            """);

        migrationBuilder.CreateIndex(
            name: "UX_PersonIdentifiers_LoperId",
            table: "PersonIdentifiers",
            column: "Value",
            unique: true,
            filter: "[IdentifierType] = 5");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_PersonIdentifiers_LoperId",
            table: "PersonIdentifiers");

        migrationBuilder.Sql(
            "DELETE FROM [PersonIdentifiers] WHERE [IdentifierType] = 5;");

        migrationBuilder.DropSequence(name: "LoperIdSequence");
    }
}
