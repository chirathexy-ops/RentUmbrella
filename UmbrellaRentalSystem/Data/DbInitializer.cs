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
            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[Transactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [Transactions] (
        [Transaction_ID] int NOT NULL IDENTITY,
        [Account_ID] int NOT NULL,
        [Umbrella_ID] nvarchar(450) NOT NULL,
        [LendDate] datetime2 NOT NULL,
        [ReturnDate] datetime2 NULL,
        [LendLocation_ID] int NOT NULL,
        [ReturnLocation_ID] int NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([Transaction_ID])
    );
END");

            context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[LostReports]', N'U') IS NULL
BEGIN
    CREATE TABLE [LostReports] (
        [Report_ID] int NOT NULL IDENTITY,
        [Transaction_ID] int NOT NULL,
        [Report_Date] datetime2 NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsProcessed] bit NOT NULL,
        CONSTRAINT [PK_LostReports] PRIMARY KEY ([Report_ID])
    );
END");
        }
    }
}
