using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;
using Microsoft.AspNetCore.Http; // 確保有這一行才能使用 GetString

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
        // GET: Umbrellas
        public async Task<IActionResult> Index(int? locationId)
        {
            ViewBag.IsManager = IsManager();

            // 1. 抓出所有地點，供側邊欄使用
            ViewBag.Locations = await _context.Locations.ToListAsync();

            // 2. 抓出雨傘資料，並強制把「地點」與「贊助商」的關聯資料表一起連進來（重點！）
            var umbrellas = _context.Umbrellas
                                    .Include(u => u.Location) // 👈 負責抓地點名稱，解決「女二宿」與篩選問題
                                    .Include(u => u.Sponsor)  // 👈 負責抓贊助商名稱，解決顯示「無」的問題
                                    .AsQueryable();

            // 3. 如果有傳入地點 ID，就進行篩選
            if (locationId.HasValue)
            {
                umbrellas = umbrellas.Where(u => u.LocationId == locationId);
                ViewBag.CurrentLocation = locationId;
            }

            return View(await umbrellas.ToListAsync());
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
            // --- 安全檢查 ---
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
            // --- 安全檢查 ---
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
            // --- 安全檢查 ---
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
            // --- 安全檢查 ---
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
            // --- 安全檢查 ---
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
            // --- 安全檢查 ---
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

        // GET: Umbrellas/Status
        public async Task<IActionResult> Status()
        {
            // 🎯 關鍵修改：補上 .Include(u => u.Sponsor)，這樣前端卡片才能用 @item.Sponsor.SponsorName 抓到名字！
            var umbrellas = await _context.Umbrellas
                                          .Include(u => u.Sponsor)
                                          .OrderBy(u => u.UmbrellaId)
                                          .ToListAsync();
            return View(umbrellas);
        }
    }
}
