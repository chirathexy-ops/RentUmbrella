using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;

namespace UmbrellaRentalSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. 資料庫連線設定
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // 2. 【關鍵修改】移除內建的 Identity 設定
            // 我們改用 Session 來處理登入
            builder.Services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // 登入有效時間 30 分鐘
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddControllersWithViews();
            // 如果你還有在使用一些內建頁面，可以暫留，但通常自建系統不需要 MapRazorPages
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // 3. 執行資料庫種子 (Seed Data)
            // 這會在每次啟動時檢查是否需要補齊管理員與贊助商
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    DbInitializer.Initialize(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "初始化資料庫時發生錯誤。");
                }
            }

            // 4. 設定 HTTP 請求管道
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(); // 舊版叫 MapStaticAssets，標準寫法建議用 UseStaticFiles

            app.UseRouting();

            // 5. 【關鍵修改】啟用 Session
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                // 取得資料庫 context
                var context = services.GetRequiredService<ApplicationDbContext>();

                // 確保資料庫存在
                context.Database.EnsureCreated();

                // 檢查 admin 是否存在
                if (!context.Accounts.Any(a => a.Name == "admin"))
                {
                    var admin = new Account
                    {
                        Name = "admin",
                        Password = "1234",
                        Email = "admin@gmail.com",
                        Phone = "0912345678",
                        EasyCard = "00000000",
                        Role = "Manager"
                    };

                    context.Accounts.Add(admin);

                    context.SaveChanges();
                }
            }
            app.Run();
        }
    }
}