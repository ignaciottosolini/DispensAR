using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispensAR.Api.Data.Migrations;

public partial class NumericTenantIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // A cast cannot convert demo/acme. Build one mapping for all existing rows.
        // EF runs these commands in its migration transaction on the same connection.
        migrationBuilder.Sql("""
            CREATE TABLE #TenantMap (OldId nvarchar(63) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY, NewId int NOT NULL UNIQUE);
            INSERT INTO #TenantMap (OldId, NewId)
                SELECT Id, CASE WHEN Id = N'demo' THEN 1 ELSE 2 END
                FROM dbo.Organizations WHERE Id IN (N'demo', N'acme');
            INSERT INTO #TenantMap (OldId, NewId)
                SELECT Id, CAST(ROW_NUMBER() OVER (ORDER BY Id) + 2 AS int)
                FROM dbo.Organizations WHERE Id NOT IN (N'demo', N'acme');
            ALTER TABLE dbo.Organizations ADD NumericId int NULL;
            ALTER TABLE dbo.Branches ADD NumericTenantId int NULL;
            ALTER TABLE dbo.Users ADD NumericTenantId int NULL;
            ALTER TABLE dbo.AspNetUsers ADD NumericTenantId int NULL;
            """);
        migrationBuilder.Sql("""
            UPDATE o SET NumericId = m.NewId FROM dbo.Organizations o JOIN #TenantMap m ON m.OldId = o.Id;
            UPDATE b SET NumericTenantId = m.NewId FROM dbo.Branches b JOIN #TenantMap m ON m.OldId = b.TenantId;
            UPDATE u SET NumericTenantId = m.NewId FROM dbo.Users u JOIN #TenantMap m ON m.OldId = u.TenantId;
            UPDATE a SET NumericTenantId = m.NewId FROM dbo.AspNetUsers a JOIN #TenantMap m ON m.OldId = a.TenantId;
            ALTER TABLE dbo.Organizations ALTER COLUMN NumericId int NOT NULL;
            ALTER TABLE dbo.Branches ALTER COLUMN NumericTenantId int NOT NULL;
            ALTER TABLE dbo.Users ALTER COLUMN NumericTenantId int NOT NULL;
            ALTER TABLE dbo.AspNetUsers ALTER COLUMN NumericTenantId int NOT NULL;

            ALTER TABLE dbo.Branches DROP CONSTRAINT FK_Branches_Organizations_TenantId;
            ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_Organizations_TenantId;
            ALTER TABLE dbo.AspNetUsers DROP CONSTRAINT FK_AspNetUsers_Organizations_TenantId;
            ALTER TABLE dbo.Branches DROP CONSTRAINT PK_Branches;
            ALTER TABLE dbo.Users DROP CONSTRAINT PK_Users;
            ALTER TABLE dbo.Organizations DROP CONSTRAINT PK_Organizations;
            DROP INDEX IX_Users_TenantId_Email ON dbo.Users;
            DROP INDEX IX_AspNetUsers_TenantId ON dbo.AspNetUsers;
            ALTER TABLE dbo.Branches DROP COLUMN TenantId;
            ALTER TABLE dbo.Users DROP COLUMN TenantId;
            ALTER TABLE dbo.AspNetUsers DROP COLUMN TenantId;
            ALTER TABLE dbo.Organizations DROP COLUMN Id;
            """);
        migrationBuilder.RenameColumn("NumericId", "Organizations", "Id");
        migrationBuilder.RenameColumn("Name", "Organizations", "Descripcion");
        foreach (var table in new[] { "Branches", "Users", "AspNetUsers" })
            migrationBuilder.RenameColumn("NumericTenantId", table, "TenantId");
        migrationBuilder.CreateSequence<int>(name: "TenantNumbers", startValue: 3L);
        migrationBuilder.Sql("""
            ALTER TABLE dbo.Organizations ADD CONSTRAINT PK_Organizations PRIMARY KEY (Id);
            ALTER TABLE dbo.Branches ADD CONSTRAINT PK_Branches PRIMARY KEY (TenantId, Id);
            ALTER TABLE dbo.Users ADD CONSTRAINT PK_Users PRIMARY KEY (TenantId, Id);
            ALTER TABLE dbo.Branches ADD CONSTRAINT FK_Branches_Organizations_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Organizations(Id);
            ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Organizations_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Organizations(Id);
            ALTER TABLE dbo.AspNetUsers ADD CONSTRAINT FK_AspNetUsers_Organizations_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Organizations(Id);
            CREATE UNIQUE INDEX IX_Users_TenantId_Email ON dbo.Users(TenantId, Email);
            CREATE INDEX IX_AspNetUsers_TenantId ON dbo.AspNetUsers(TenantId);
            ALTER TABLE dbo.Organizations ADD CONSTRAINT DF_Organizations_Id DEFAULT (NEXT VALUE FOR dbo.TenantNumbers) FOR Id;
            DECLARE @next bigint = (SELECT COALESCE(MAX(CONVERT(bigint, NewId)), 2) + 1 FROM #TenantMap);
            IF @next < 3 SET @next = 3;
            DECLARE @restart nvarchar(200) = N'ALTER SEQUENCE dbo.TenantNumbers RESTART WITH ' + CAST(@next AS nvarchar(20));
            EXEC(@restart);
            DROP TABLE #TenantMap;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("La conversion de tenants a int no tiene rollback automatico. Restaurar un backup previo junto con la version anterior de la aplicacion.");
    }
}
