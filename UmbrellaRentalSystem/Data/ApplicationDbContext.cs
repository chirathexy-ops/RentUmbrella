using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Models; // 確保有引用 Models 命名空間

namespace UmbrellaRentalSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 加入這一行，讓程式知道資料庫裡有 Accounts 這張表
        public DbSet<Account> Accounts { get; set; }

        // 其他原本有的 DbSet 也要統一改成複數（配合我們剛改好的資料庫名稱）
        public DbSet<Umbrella> Umbrellas { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<LostReport> LostReports { get; set; }
        public DbSet<UmbrellaRentalSystem.Models.Account> Manager { get; set; } = default!;
    }
}