using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UmbrellaRentalSystem.Data;
using UmbrellaRentalSystem.Models;
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

            HttpContext.Session.SetInt32("AccountId", account.Account_ID);
            HttpContext.Session.SetString("Username", account.Name);
            HttpContext.Session.SetString("Role", account.Role ?? "User");

            if ((account.Role ?? "").ToLower() == "manager")
            {
                return RedirectToAction("Index", "Umbrellas");
            }
            return RedirectToAction("Index", "Home");
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
            // ⭕ 改用 Account_ID 來查詢
            var account = await _context.Accounts.FindAsync(Account_ID);
            if (account != null)
            {
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}