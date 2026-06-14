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
        public async Task<IActionResult> Index(int? locationId)
        {
            // 1. 抓出所有地點，供側邊欄使用
            ViewBag.Locations = await _context.Locations.ToListAsync();

            // 2. 抓出雨傘資料
            var umbrellas = _context.Umbrellas.AsQueryable();

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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminName")))
            {
                return RedirectToAction("Login", "Managers");
            }
            return View();
        }

        // POST: Umbrellas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Status,SponsorId")] Umbrella umbrella)
        {
            // --- 安全檢查 ---
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminName")))
            {
                return RedirectToAction("Login", "Managers");
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminName")))
            {
                return RedirectToAction("Login", "Managers");
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminName")))
            {
                return RedirectToAction("Login", "Managers");
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminName")))
            {
                return RedirectToAction("Login", "Managers");
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminName")))
            {
                return RedirectToAction("Login", "Managers");
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

        public async Task<IActionResult> Status()
        {
            // 增加 .OrderBy(u => u.Id)，讓雨傘編號排好
            var umbrellas = await _context.Umbrellas.OrderBy(u => u.UmbrellaId).ToListAsync();
            return View(umbrellas);
        }
    }
}