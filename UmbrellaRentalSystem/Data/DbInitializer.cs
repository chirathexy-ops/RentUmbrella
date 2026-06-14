using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;
using System.Linq;

namespace UmbrellaRentalSystem.Data
{
    // 必須要有這一行 class 定義，否則會報 CS0116 錯誤
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // 確保資料庫已根據目前的 Model 建立
            context.Database.EnsureCreated();

            // 1. 塞入贊助商：對應新的 Sponsor_Name
            // 注意：因為 Sponsor_ID 是 IDENTITY(1,1)，所以這裡絕對不能寫 Id = ...
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
    }
}