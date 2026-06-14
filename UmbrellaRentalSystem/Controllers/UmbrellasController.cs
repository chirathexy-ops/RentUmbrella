using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;
using Microsoft.AspNetCore.Http; // 確保有這一行才能使用 GetString 和 GetInt32

namespace UmbrellaRentalSystem.Controllers
{
    public class UmbrellasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UmbrellasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Umbrellas
        public async Task<IActionResult> Index(int? locationId)
        {
            ViewBag.IsManager = IsManager();

            // 檢查一般使用者是否登入，並傳給前端判斷
            ViewBag.IsUserLoggedIn = IsUser();

            // 1. 抓出所有地點，供側邊欄使用
            ViewBag.Locations = await _context.Locations.ToListAsync();

            // 2. 抓出雨傘資料，並強制把「地點」與「贊助商」的關聯資料表一起連進來
            var umbrellas = _context.Umbrellas
                                    .Include(u => u.Location)
                                    .Include(u => u.Sponsor)
                                    .AsQueryable();

            // 3. 如果有傳入地點 ID，就進行篩選
            if (locationId.HasValue)
            {
                umbrellas = umbrellas.Where(u => u.LocationId == locationId);
                ViewBag.CurrentLocation = locationId;
            }

            return View(await umbrellas.ToListAsync());
        }

        // POST: Umbrellas/RentUmbrella (一般使用者直接點擊借傘)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RentUmbrella(string umbrellaId)
        {
            var currentUsername = HttpContext.Session.GetString("Username");
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (!IsUser() || string.IsNullOrEmpty(currentUsername) || !accountId.HasValue)
            {
                TempData["ErrorMessage"] = "請先登入系統才能借傘！";
                return RedirectToAction("UserLogin", "Accounts");
            }

            if (string.IsNullOrEmpty(umbrellaId))
            {
                return NotFound();
            }

            var umbrella = await _context.Umbrellas.FirstOrDefaultAsync(u => u.UmbrellaId == umbrellaId);
            if (umbrella == null || umbrella.Status != "Available")
            {
                TempData["ErrorMessage"] = "很抱歉，這把雨傘目前無法借用（可能已被借走）。";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // 🔄 變更雨傘狀態
                umbrella.Status = "Rented";

                if (accountId.HasValue)
                {
                    _context.Transactions.Add(new Transaction
                    {
                        Account_ID = accountId.Value,
                        Umbrella_ID = umbrella.UmbrellaId,
                        LendDate = DateTime.Now,
                        LendLocation_ID = umbrella.LocationId,
                        Status = "Active"
                    });
                }

                _context.Update(umbrella);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"🎉 借傘成功！已成功借用雨傘編號 #{umbrellaId}。";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "借傘程序發生錯誤，請稍後再試。";
            }

            return RedirectToAction(nameof(Index), new { locationId = umbrella.LocationId });
        }

        // 🔄 修正後的 GET: Umbrellas/ReturnUmbrella (讓使用者只看見自己目前借的傘)
        public async Task<IActionResult> ReturnUmbrella(string? umbrellaId)
        {
            if (!IsUser())
            {
                TempData["ErrorMessage"] = "請先登入使用者帳號才能還傘！";
                return RedirectToAction("UserLogin", "Accounts");
            }

            // 🔑 取得目前登入使用者的 AccountId
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (!accountId.HasValue)
            {
                TempData["ErrorMessage"] = "無法辨識使用者身份，請重新登入。";
                return RedirectToAction("UserLogin", "Accounts");
            }

            // 🔍 1. 從 Transactions 資料表中，精準找出該使用者目前未歸還 (Status == "Active") 的借傘紀錄
            var myActiveUmbrellaIds = await _context.Transactions
                .Where(t => t.Account_ID == accountId.Value && t.Status == "Active" && t.ReturnDate == null)
                .Select(t => t.Umbrella_ID)
                .ToListAsync();

            // ☔ 2. 用篩選出來的 UmbrellaId 清單，去抓出對應的雨傘詳細資料（包含贊助商資訊，供前端顯示）
            var myRentedUmbrellas = await _context.Umbrellas
                .Include(u => u.Sponsor)
                .Where(u => myActiveUmbrellaIds.Contains(u.UmbrellaId))
                .OrderBy(u => u.UmbrellaId)
                .ToListAsync();

            // 💡 3. 將資料包裝好傳給 View
            ViewBag.MyRentedUmbrellasList = myRentedUmbrellas; // 傳送實體清單，方便前端刻出漂亮的表格
            ViewBag.SelectedUmbrellaId = umbrellaId;

            // 下拉選單也只會出現該使用者自己借的傘
            ViewBag.RentedUmbrellas = new SelectList(myRentedUmbrellas, "UmbrellaId", "UmbrellaId", umbrellaId);

            // 抓出所有可歸還的站點位置
            ViewBag.Locations = new SelectList(await _context.Locations.OrderBy(l => l.LocationName).ToListAsync(), "LocationId", "LocationName");

            return View();
        }

        // POST: Umbrellas/ReturnUmbrella (送出還傘地點)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnUmbrella(string umbrellaId, int returnLocationId)
        {
            var currentUsername = HttpContext.Session.GetString("Username");
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (!IsUser() || string.IsNullOrEmpty(currentUsername) || !accountId.HasValue)
            {
                TempData["ErrorMessage"] = "請先登入系統才能還傘！";
                return RedirectToAction("UserLogin", "Accounts");
            }

            if (string.IsNullOrEmpty(umbrellaId))
            {
                return NotFound();
            }

            var umbrella = await _context.Umbrellas.FirstOrDefaultAsync(u => u.UmbrellaId == umbrellaId);
            if (umbrella == null || umbrella.Status != "Rented")
            {
                TempData["ErrorMessage"] = "錯誤：這把雨傘目前並非被借出狀態，無法歸還。";
                return RedirectToAction(nameof(ReturnUmbrella));
            }

            var activeTransaction = await _context.Transactions
                .Where(t => t.Account_ID == accountId.Value
                    && t.Umbrella_ID == umbrellaId
                    && t.Status == "Active"
                    && t.ReturnDate == null)
                .OrderByDescending(t => t.LendDate)
                .FirstOrDefaultAsync();

            if (activeTransaction == null)
            {
                TempData["ErrorMessage"] = "這把雨傘不是你目前借出的雨傘，無法歸還。";
                return RedirectToAction(nameof(ReturnUmbrella));
            }

            var returnLocation = await _context.Locations.FindAsync(returnLocationId);
            if (returnLocation == null)
            {
                TempData["ErrorMessage"] = "請選擇有效的還傘地點。";
                return RedirectToAction(nameof(ReturnUmbrella), new { umbrellaId });
            }

            try
            {
                // 🔄 變更狀態為在庫，並更新它被還到了哪一個站點
                umbrella.Status = "Available";
                umbrella.LocationId = returnLocationId;

                activeTransaction.ReturnDate = DateTime.Now;
                activeTransaction.ReturnLocation_ID = returnLocationId;
                activeTransaction.Status = "Completed";
                _context.Update(activeTransaction);

                _context.Update(umbrella);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"還傘成功！雨傘編號 #{umbrellaId} 已歸還至 {returnLocation.LocationName}。";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "還傘程序發生錯誤，請稍後再試。";
            }

            return RedirectToAction(nameof(Index), new { locationId = returnLocationId });
        }

        // GET: Umbrellas/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var umbrella = await _context.Umbrellas
                .FirstOrDefaultAsync(m => m.UmbrellaId == id);
            if (umbrella == null)
            {
                return NotFound();
            }

            return View(umbrella);
        }

        // GET: Umbrellas/Create
        public IActionResult Create()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }
            return View();
        }

        // POST: Umbrellas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Status,SponsorId")] Umbrella umbrella)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (ModelState.IsValid)
            {
                _context.Add(umbrella);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(umbrella);
        }

        // GET: Umbrellas/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (id == null)
            {
                return NotFound();
            }

            var umbrella = await _context.Umbrellas.FindAsync(id);
            if (umbrella == null)
            {
                return NotFound();
            }
            return View(umbrella);
        }

        // POST: Umbrellas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Status,SponsorId")] Umbrella umbrella)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (id != umbrella.UmbrellaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(umbrella);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UmbrellaExists(umbrella.UmbrellaId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(umbrella);
        }

        // GET: Umbrellas/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (id == null)
            {
                return NotFound();
            }

            var umbrella = await _context.Umbrellas
                .FirstOrDefaultAsync(m => m.UmbrellaId == id);
            if (umbrella == null)
            {
                return NotFound();
            }

            return View(umbrella);
        }

        // POST: Umbrellas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            var umbrella = await _context.Umbrellas.FindAsync(id);
            if (umbrella != null)
            {
                _context.Umbrellas.Remove(umbrella);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UmbrellaExists(string id)
        {
            return _context.Umbrellas.Any(e => e.UmbrellaId == id);
        }

        private bool IsManager()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"))
                && string.Equals(HttpContext.Session.GetString("Role"), "Manager", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsUser()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"))
                && string.Equals(HttpContext.Session.GetString("Role"), "User", StringComparison.OrdinalIgnoreCase);
        }

        // GET: Umbrellas/Status
        public async Task<IActionResult> Status()
        {
            var umbrellas = await _context.Umbrellas
                                          .Include(u => u.Sponsor)
                                          .OrderBy(u => u.UmbrellaId)
                                          .ToListAsync();
            return View(umbrellas);
        }
    }
}
