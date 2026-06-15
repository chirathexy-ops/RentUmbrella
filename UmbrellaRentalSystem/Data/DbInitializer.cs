using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Models;

namespace UmbrellaRentalSystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();
            EnsureTransactionTables(context);

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

        private static void EnsureTransactionTables(ApplicationDbContext context)
        {
            RenameColumnIfExists(context, "Umbrellas", "Umbrella_ID", "UmbrellaId");
            RenameColumnIfExists(context, "Umbrellas", "Location_ID", "LocationId");
            RenameColumnIfExists(context, "Umbrellas", "Sponsor_ID", "SponsorId");
            RenameColumnIfExists(context, "Transactions", "Transaction_ID", "TransactionId");
            RenameColumnIfExists(context, "Transactions", "Account_ID", "AccountId");
            RenameColumnIfExists(context, "Transactions", "Umbrella_ID", "UmbrellaId");
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
