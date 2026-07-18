using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;

namespace UmbrellaRentalSystem.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            return View(await _context.Accounts.ToListAsync());
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Name == username && u.Password == password);

            if (account == null)
            {
                ModelState.AddModelError("", "帳號或密碼錯誤");
                return View();
            }

            if (!string.Equals(account.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "此入口限管理員登入，使用者請改用使用者登入。");
                return View();
            }

            HttpContext.Session.SetInt32("AccountId", account.AccountId);
            HttpContext.Session.SetString("Username", account.Name);
            HttpContext.Session.SetString("Role", account.Role ?? "User");

            return RedirectToAction("Index", "Umbrellas");
        }

        public IActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserLogin(string loginId, string password)
        {
            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "請輸入帳號或 Email 與密碼");
                return View();
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(u =>
                    (u.Name == loginId || u.Email == loginId)
                    && u.Password == password
                    && u.Role != "Manager");

            if (account == null)
            {
                ModelState.AddModelError("", "帳號、Email 或密碼錯誤，請先確認資料或註冊使用者帳號。");
                return View();
            }

            HttpContext.Session.SetInt32("AccountId", account.AccountId);
            HttpContext.Session.SetString("Username", account.Name);
            HttpContext.Session.SetString("Role", account.Role ?? "User");
            HttpContext.Session.SetString("EasyCard", account.EasyCard ?? "");

            return RedirectToAction(nameof(UserPage));
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Name,Password,Email,Phone,EasyCard")] Account account)
        {
            if (await _context.Accounts.AnyAsync(a => a.Name == account.Name))
            {
                ModelState.AddModelError(nameof(account.Name), "此帳號名稱已被使用");
            }

            if (!string.IsNullOrWhiteSpace(account.Email)
                && await _context.Accounts.AnyAsync(a => a.Email == account.Email))
            {
                ModelState.AddModelError(nameof(account.Email), "此 Email 已被註冊");
            }

            if (!string.IsNullOrWhiteSpace(account.EasyCard)
                && await _context.Accounts.AnyAsync(a => a.EasyCard == account.EasyCard))
            {
                ModelState.AddModelError(nameof(account.EasyCard), "此卡號已被註冊");
            }

            if (!ModelState.IsValid)
            {
                return View(account);
            }

            account.Role = "User";
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("AccountId", account.AccountId);
            HttpContext.Session.SetString("Username", account.Name);
            HttpContext.Session.SetString("Role", account.Role);
            HttpContext.Session.SetString("EasyCard", account.EasyCard ?? "");

            return RedirectToAction(nameof(UserPage));
        }

        public IActionResult UserPage()
        {
            if (!IsUser())
            {
                return RedirectToAction(nameof(UserLogin));
            }

            return View();
        }

        public IActionResult Create()
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Password,Email,Phone,EasyCard,Role")] Account account)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (ModelState.IsValid)
            {
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(account);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (id == null) return NotFound();

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AccountId,Name,Password,Email,Phone,EasyCard,Role")] Account account)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (id != account.AccountId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(account);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Accounts.Any(e => e.AccountId == account.AccountId)) return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(account);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (id == null) return NotFound();

            var account = await _context.Accounts.FirstOrDefaultAsync(m => m.AccountId == id);
            if (account == null) return NotFound();

            return View(account);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (id == null) return NotFound();

            var account = await _context.Accounts.FirstOrDefaultAsync(m => m.AccountId == id);
            if (account == null) return NotFound();

            return View(account);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            var account = await _context.Accounts.FindAsync(id);
            if (account != null)
            {
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
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