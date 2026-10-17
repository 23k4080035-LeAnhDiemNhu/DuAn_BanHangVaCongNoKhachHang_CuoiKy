using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DuAn_BanHangVaCongNoKhachHang.Pages;

[ValidateAntiForgeryToken]
public class LogoutModel : PageModel
{
    // Hiển thị trang xác nhận đăng xuất; từ chối thực hiện đăng xuất trên GET
    public IActionResult OnGet()
    {
        return Page();
    }

    // Đăng xuất bằng HTTP POST có antiforgery token
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }
}
