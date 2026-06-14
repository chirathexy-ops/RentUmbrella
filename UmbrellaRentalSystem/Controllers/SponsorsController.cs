using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;

namespace UmbrellaRentalSystem.Controllers
{
    public class SponsorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SponsorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sponsors
        public async Task<IActionResult> Index()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            return View(await _context.Sponsors.ToListAsync());
        }

        // GET: Sponsors/Details/1
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (id == null) return NotFound();

            // 修正：對應 Model 的 SponsorId
            var sponsor = await _context.Sponsors
                .FirstOrDefaultAsync(m => m.SponsorId == id);

            if (sponsor == null) return NotFound();

            return View(sponsor);
        }

        public IActionResult Create()
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // 修正：只綁定 SponsorName
        public async Task<IActionResult> Create([Bind("SponsorName")] Sponsor sponsor)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (ModelState.IsValid)
            {
                _context.Add(sponsor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(sponsor);
        }

        // GET: Sponsors/Edit/1
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (id == null) return NotFound();

            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null) return NotFound();
            return View(sponsor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // 修正：綁定 SponsorId, SponsorName
        public async Task<IActionResult> Edit(int id, [Bind("SponsorId,SponsorName")] Sponsor sponsor)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (id != sponsor.SponsorId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sponsor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SponsorExists(sponsor.SponsorId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(sponsor);
        }

        // GET: Sponsors/Delete/1
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (id == null) return NotFound();

            var sponsor = await _context.Sponsors
                .FirstOrDefaultAsync(m => m.SponsorId == id);
            if (sponsor == null) return NotFound();

            return View(sponsor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsManager())
            {
                return RedirectToAction("Login", "Accounts");
            }

            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor != null)
            {
                _context.Sponsors.Remove(sponsor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SponsorExists(int id)
        {
            return _context.Sponsors.Any(e => e.SponsorId == id);
        }

        private bool IsManager()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"))
                && string.Equals(HttpContext.Session.GetString("Role"), "Manager", StringComparison.OrdinalIgnoreCase);
        }
    }
}
