using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace UmbrellaRentalSystem.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Accounts
        public async Task<IActionResult> Index()
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            return View(await _context.Accounts.ToListAsync());
        }

        // GET: /Accounts/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Accounts/Login
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

            HttpContext.Session.SetInt32("AccountId", account.Account_ID);
            HttpContext.Session.SetString("Username", account.Name);
            HttpContext.Session.SetString("Role", account.Role ?? "User");

            return RedirectToAction("Index", "Umbrellas");
        }

        // GET: /Accounts/UserLogin
        public IActionResult UserLogin()
        {
            return View();
        }

        // POST: /Accounts/UserLogin
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

            HttpContext.Session.SetInt32("AccountId", account.Account_ID);
            HttpContext.Session.SetString("Username", account.Name);
            HttpContext.Session.SetString("Role", account.Role ?? "User");
            HttpContext.Session.SetString("EasyCard", account.EasyCard ?? "");

            return RedirectToAction(nameof(UserPage));
        }

        // GET: /Accounts/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Accounts/Register
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

            HttpContext.Session.SetInt32("AccountId", account.Account_ID);
            HttpContext.Session.SetString("Username", account.Name);
            HttpContext.Session.SetString("Role", account.Role);
            HttpContext.Session.SetString("EasyCard", account.EasyCard ?? "");

            return RedirectToAction(nameof(UserPage));
        }

        // GET: /Accounts/UserPage
        public IActionResult UserPage()
        {
            if (!IsUser())
            {
                return RedirectToAction(nameof(UserLogin));
            }

            return View();
        }

        // GET: /Accounts/Create
        public IActionResult Create()
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }

        // POST: /Accounts/Create
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

        // GET: /Accounts/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: /Accounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (id == null) return NotFound();

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            return View(account); // ⭕ 這樣才能成功開啟 Edit.cshtml 畫面
        }

        // POST: /Accounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Account_ID,Name,Password,Email,Phone,EasyCard,Role")] Account account)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (id != account.Account_ID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(account);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Accounts.Any(e => e.Account_ID == account.Account_ID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(account);
        }

        // GET: /Accounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            if (id == null) return NotFound();

            var account = await _context.Accounts
                .FirstOrDefaultAsync(m => m.Account_ID == id);
            if (account == null) return NotFound();

            return View(account); // ⭕ 這樣才能成功開啟 Delete.cshtml 畫面
        }

        // POST: /Accounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        // ⭕ 核心修正：把參數名稱從小寫的 id 改成 Account_ID，這樣才能對應到前端傳過來的值！
        public async Task<IActionResult> DeleteConfirmed(int Account_ID)
        {
            if (!IsManager())
            {
                return RedirectToAction(nameof(Login));
            }

            // ⭕ 改用 Account_ID 來查詢
            var account = await _context.Accounts.FindAsync(Account_ID);
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
