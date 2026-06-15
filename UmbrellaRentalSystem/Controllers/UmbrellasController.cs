using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;

namespace UmbrellaRentalSystem.Controllers
{
    public class UmbrellasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UmbrellasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? locationId)
        {
            ViewBag.IsManager = IsManager();
            ViewBag.IsUserLoggedIn = IsUser();
            ViewBag.Locations = await _context.Locations.ToListAsync();

            var umbrellas = _context.Umbrellas
                .Include(u => u.Location)
                .Include(u => u.Sponsor)
                .AsQueryable();

            if (locationId.HasValue)
            {
                umbrellas = umbrellas.Where(u => u.LocationId == locationId.Value);
                ViewBag.CurrentLocation = locationId;
            }

            return View(await umbrellas.OrderBy(u => u.UmbrellaId).ToListAsync());
        }

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
            if (umbrella == null || !(string.Equals(umbrella.Status, "Available", StringComparison.OrdinalIgnoreCase) || umbrella.Status == "在庫" || umbrella.Status == "?典澈"))
            {
                TempData["ErrorMessage"] = "很抱歉，這把雨傘目前無法借用。";
                return RedirectToAction(nameof(Index));
            }

            var lendLocationId = umbrella.LocationId;

            try
            {
                umbrella.Status = "Rented";

                _context.Transactions.Add(new Transaction
                {
                    AccountId = accountId.Value,
                    UmbrellaId = umbrella.UmbrellaId,
                    LendDate = DateTime.Now,
                    LendLocationId = lendLocationId,
                    Status = "Active"
                });

                _context.Update(umbrella);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"借傘成功！已成功借用雨傘編號 #{umbrellaId}。";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "借傘程序發生錯誤，請稍後再試。";
            }

            return RedirectToAction(nameof(Index), new { locationId = lendLocationId });
        }

        public async Task<IActionResult> ReturnUmbrella(string? umbrellaId)
        {
            if (!IsUser())
            {
                TempData["ErrorMessage"] = "請先登入使用者帳號才能還傘！";
                return RedirectToAction("UserLogin", "Accounts");
            }

            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (!accountId.HasValue)
            {
                TempData["ErrorMessage"] = "無法辨識使用者身份，請重新登入。";
                return RedirectToAction("UserLogin", "Accounts");
            }

            var myActiveUmbrellaIds = await _context.Transactions
                .Where(t => t.AccountId == accountId.Value && t.Status == "Active" && t.ReturnDate == null)
                .Select(t => t.UmbrellaId)
                .ToListAsync();

            var myRentedUmbrellas = new List<Umbrella>();
            if (myActiveUmbrellaIds.Any())
            {
                myRentedUmbrellas = await _context.Umbrellas
                    .Include(u => u.Sponsor)
                    .Where(u => myActiveUmbrellaIds.Contains(u.UmbrellaId))
                    .OrderBy(u => u.UmbrellaId)
                    .ToListAsync();
            }

            ViewBag.MyRentedUmbrellasList = myRentedUmbrellas;
            ViewBag.SelectedUmbrellaId = umbrellaId;
            ViewBag.RentedUmbrellas = new SelectList(myRentedUmbrellas, "UmbrellaId", "UmbrellaId", umbrellaId);
            ViewBag.Locations = new SelectList(
                await _context.Locations.OrderBy(l => l.LocationName).ToListAsync(),
                "LocationId",
                "LocationName");

            return View();
        }

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
                .Where(t => t.AccountId == accountId.Value
                    && t.UmbrellaId == umbrellaId
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
                umbrella.Status = "Available";
                umbrella.LocationId = returnLocationId;

                activeTransaction.ReturnDate = DateTime.Now;
                activeTransaction.ReturnLocationId = returnLocationId;
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

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var umbrella = await _context.Umbrellas
                .Include(u => u.Location)
                .Include(u => u.Sponsor)
                .FirstOrDefaultAsync(m => m.UmbrellaId == id);
            if (umbrella == null)
            {
                return NotFound();
            }

            return View(umbrella);
        }

        public IActionResult Create()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            return View(new Umbrella { UmbrellaId = string.Empty, Status = "Available" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UmbrellaId,Status,LocationId,SponsorId")] Umbrella umbrella)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (string.IsNullOrWhiteSpace(umbrella.Status))
            {
                umbrella.Status = "Available";
            }

            if (ModelState.IsValid)
            {
                _context.Add(umbrella);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(umbrella);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("UmbrellaId,Status,LocationId,SponsorId")] Umbrella umbrella)
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

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(umbrella);
        }

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
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Status()
        {
            var umbrellas = await _context.Umbrellas
                .Include(u => u.Sponsor)
                .Include(u => u.Location)
                .OrderBy(u => u.UmbrellaId)
                .ToListAsync();

            return View(umbrellas);
        }

        public async Task<IActionResult> ReportLost()
        {
            if (!IsUser())
            {
                TempData["ErrorMessage"] = "請先登入使用者帳號才能通報遺失！";
                return RedirectToAction("UserLogin", "Accounts");
            }

            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (!accountId.HasValue)
            {
                TempData["ErrorMessage"] = "無法辨識使用者身份，請重新登入。";
                return RedirectToAction("UserLogin", "Accounts");
            }

            var myActiveUmbrellaIds = await _context.Transactions
                .Where(t => t.AccountId == accountId.Value && t.Status == "Active" && t.ReturnDate == null)
                .Select(t => t.UmbrellaId)
                .ToListAsync();

            var myRentedUmbrellas = await _context.Umbrellas
                .Where(u => myActiveUmbrellaIds.Contains(u.UmbrellaId))
                .ToListAsync();

            ViewBag.MyRentedUmbrellas = new SelectList(myRentedUmbrellas, "UmbrellaId", "UmbrellaId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportLost(string umbrellaId, string description)
        {
            if (!IsUser())
            {
                return RedirectToAction("UserLogin", "Accounts");
            }

            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (!accountId.HasValue)
            {
                return RedirectToAction("UserLogin", "Accounts");
            }

            if (string.IsNullOrEmpty(umbrellaId))
            {
                TempData["ErrorMessage"] = "請選擇遺失的雨傘。";
                return RedirectToAction(nameof(ReportLost));
            }

            var activeTransaction = await _context.Transactions
                .Where(t => t.UmbrellaId == umbrellaId
                    && t.AccountId == accountId.Value
                    && t.Status == "Active"
                    && t.ReturnDate == null)
                .FirstOrDefaultAsync();

            if (activeTransaction == null)
            {
                TempData["ErrorMessage"] = "找不到該把借用中的雨傘紀錄，無法通報遺失。";
                return RedirectToAction(nameof(ReportLost));
            }

            var umbrella = await _context.Umbrellas.FirstOrDefaultAsync(u => u.UmbrellaId == umbrellaId);
            if (umbrella == null)
            {
                return NotFound();
            }

            try
            {
                umbrella.Status = "Lost";
                _context.Update(umbrella);

                activeTransaction.Status = "Lost";
                activeTransaction.ReturnDate = DateTime.Now;
                _context.Update(activeTransaction);

                _context.LostReports.Add(new LostReport
                {
                    TransactionId = activeTransaction.TransactionId,
                    ReportDate = DateTime.Now,
                    Description = description ?? "未提供詳細說明",
                    IsProcessed = false
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"雨傘 #{umbrellaId} 遺失通報成功。";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "通報程序發生錯誤，請稍後再試。";
            }

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
    }
}