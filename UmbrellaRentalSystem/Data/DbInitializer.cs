using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Models;

namespace UmbrellaRentalSystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();
            EnsureSchema(context);

            if (!context.Sponsors.Any())
            {
                context.Sponsors.AddRange(
                    new Sponsor { SponsorName = "慈濟基金會" },
                    new Sponsor { SponsorName = "台灣世界展望會" },
                    new Sponsor { SponsorName = "遠東新世紀" }
                );
                context.SaveChanges();
            }
        }

        private static void EnsureSchema(ApplicationDbContext context)
        {
            RenameColumnIfExists(context, "Accounts", "Account_ID", "AccountId");
            EnsureUmbrellaIdentitySchema(context);
            RenameColumnIfExists(context, "Transactions", "Transaction_ID", "TransactionId");
            RenameColumnIfExists(context, "Transactions", "LendLocation_ID", "LendLocationId");
            RenameColumnIfExists(context, "Transactions", "ReturnLocation_ID", "ReturnLocationId");
            RenameColumnIfExists(context, "LostReports", "Report_ID", "ReportId");
            RenameColumnIfExists(context, "LostReports", "Transaction_ID", "TransactionId");
            RenameColumnIfExists(context, "LostReports", "Report_Date", "ReportDate");

            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[Transactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [Transactions] (
        [TransactionId] int NOT NULL IDENTITY,
        [AccountId] int NOT NULL,
        [UmbrellaId] nvarchar(450) NOT NULL,
        [LendDate] datetime2 NOT NULL,
        [ReturnDate] datetime2 NULL,
        [LendLocationId] int NOT NULL,
        [ReturnLocationId] int NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([TransactionId])
    );
END");

            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[LostReports]', N'U') IS NULL
BEGIN
    CREATE TABLE [LostReports] (
        [ReportId] int NOT NULL IDENTITY,
        [TransactionId] int NOT NULL,
        [ReportDate] datetime2 NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsProcessed] bit NOT NULL,
        CONSTRAINT [PK_LostReports] PRIMARY KEY ([ReportId])
    );
END");
        }

        private static void EnsureUmbrellaIdentitySchema(ApplicationDbContext context)
        {
            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[Umbrellas]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[Umbrellas]', N'UmbrellaCode') IS NULL
    AND COL_LENGTH(N'[Umbrellas]', N'UmbrellaId') IS NOT NULL
BEGIN
    EXEC sp_rename N'[Umbrellas].[UmbrellaId]', N'UmbrellaCode', N'COLUMN';
END");

            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[Umbrellas]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[Umbrellas]', N'UmbrellaId') IS NULL
BEGIN
    CREATE TABLE [Umbrellas_New] (
        [UmbrellaId] int NOT NULL IDENTITY,
        [UmbrellaCode] nvarchar(450) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [LocationId] int NOT NULL,
        [SponsorId] int NOT NULL,
        -- Use a temporary constraint name. SQL Server constraint names are
        -- database-wide, so PK_Umbrellas is still owned by the old table until
        -- that table is dropped.
        CONSTRAINT [PK_Umbrellas_New] PRIMARY KEY ([UmbrellaId])
    );

    INSERT INTO [Umbrellas_New] ([UmbrellaCode], [Status], [LocationId], [SponsorId])
    SELECT [UmbrellaCode], [Status], [LocationId], [SponsorId]
    FROM [Umbrellas];

    DROP TABLE [Umbrellas];
    EXEC sp_rename N'[Umbrellas_New]', N'Umbrellas';
END");

            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[Umbrellas]', N'U') IS NULL
BEGIN
    CREATE TABLE [Umbrellas] (
        [UmbrellaId] int NOT NULL IDENTITY,
        [UmbrellaCode] nvarchar(450) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [LocationId] int NOT NULL,
        [SponsorId] int NOT NULL,
        CONSTRAINT [PK_Umbrellas] PRIMARY KEY ([UmbrellaId])
    );
END");
        }

        private static void RenameColumnIfExists(
            ApplicationDbContext context,
            string tableName,
            string oldColumnName,
            string newColumnName)
        {
            var tableObjectName = $"[{tableName}]";
            var oldFullColumnName = $"[{tableName}].[{oldColumnName}]";

            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID({0}, N'U') IS NOT NULL
    AND COL_LENGTH({0}, {1}) IS NOT NULL
    AND COL_LENGTH({0}, {2}) IS NULL
BEGIN
    EXEC sp_rename {3}, {2}, N'COLUMN';
END",
                tableObjectName,
                oldColumnName,
                newColumnName,
                oldFullColumnName);
        }
    }
}
