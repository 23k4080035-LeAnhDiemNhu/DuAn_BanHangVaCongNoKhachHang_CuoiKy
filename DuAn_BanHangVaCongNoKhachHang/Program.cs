using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.BaoCaoTuoiNoScreen;
using BanHang.UseCases.Common;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.PluginInterfaces.Security;
using BanHang.UseCases.SanPhamScreen;
using BanHang.UseCases.Security;
using BanHang.UseCases.ThanhToanScreen;
using DuAn_BanHangVaCongNoKhachHang.Data;
using DuAn_BanHangVaCongNoKhachHang.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WeatherForecastService>();

// Cấu hình Cookie Authentication & Authorization chuẩn ASP.NET Core
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BanHangCongNo.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false; // Tắt sliding expiration: cố định hạn tuyệt đối 8 giờ đồng bộ với claim AuthExpiresUtc
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/accessdenied";

        if (builder.Environment.IsDevelopment())
        {
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    AuthDiagnosticLogger.Log(
                        null,
                        $"[AUTH-DIAG] CookieEvents.OnRedirectToLogin kích hoạt! RequestPath='{ctx.Request.Path}', RedirectUri='{ctx.RedirectUri}', ResponseStatus={ctx.Response.StatusCode}");
                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                },
                OnSigningIn = ctx =>
                {
                    AuthDiagnosticLogger.Log(
                        null,
                        $"[AUTH-DIAG] CookieEvents.OnSigningIn: Bắt đầu phát Cookie. User='{ctx.Principal?.Identity?.Name}', Scheme='{ctx.Scheme.Name}'");
                    return Task.CompletedTask;
                },
                OnSignedIn = ctx =>
                {
                    AuthDiagnosticLogger.Log(
                        null,
                        $"[AUTH-DIAG] CookieEvents.OnSignedIn: Đã phát Cookie thành công cho User='{ctx.Principal?.Identity?.Name}'");
                    return Task.CompletedTask;
                },
                OnValidatePrincipal = ctx =>
                {
                    var isAuth = ctx.Principal?.Identity?.IsAuthenticated == true;
                    AuthDiagnosticLogger.Log(
                        null,
                        $"[AUTH-DIAG] CookieEvents.OnValidatePrincipal: Xác thực Cookie gửi lên. IsAuth={isAuth}, User='{ctx.Principal?.Identity?.Name}'");
                    return Task.CompletedTask;
                }
            };
        }
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NhanVienBanHangOnly", policy => policy.RequireRole(VaiTroConstants.NhanVienBanHang));
    options.AddPolicy("KeToanCongNoOnly", policy => policy.RequireRole(VaiTroConstants.KeToanCongNo));
    options.AddPolicy("BanHangOrKeToan", policy => policy.RequireRole(VaiTroConstants.NhanVienBanHang, VaiTroConstants.KeToanCongNo));
});

// Đăng ký SQL Connection Factory từ cấu hình
var connectionString = builder.Configuration.GetConnectionString("BanHangCongNo")
    ?? throw new InvalidOperationException("Chuỗi kết nối 'BanHangCongNo' không tìm thấy trong cấu hình.");
builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));

// Đăng ký dịch vụ Xác thực, Băm mật khẩu, Giới hạn thử sai & Ngữ cảnh người dùng
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddSingleton<ILoginRateLimiter, MemoryLoginRateLimiter>();
builder.Services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
builder.Services.AddScoped<IDangNhapUseCase, DangNhapUseCase>();
builder.Services.AddScoped<ICurrentUserService, BlazorCurrentUserService>();

// Đăng ký Repository và Use Cases (Scoped)
builder.Services.AddScoped<IKhachHangRepository, KhachHangRepository>();
builder.Services.AddScoped<IXemDanhSachKhachHangUseCase, XemDanhSachKhachHangUseCase>();
builder.Services.AddScoped<IThemKhachHangUseCase, ThemKhachHangUseCase>();
builder.Services.AddScoped<ICapNhatKhachHangUseCase, CapNhatKhachHangUseCase>();
builder.Services.AddScoped<IDoiTrangThaiKhachHangUseCase, DoiTrangThaiKhachHangUseCase>();

builder.Services.AddScoped<ISanPhamRepository, SanPhamRepository>();
builder.Services.AddScoped<IXemDanhSachSanPhamHoatDongUseCase, XemDanhSachSanPhamHoatDongUseCase>();
builder.Services.AddScoped<IXemDanhSachSanPhamUseCase, XemDanhSachSanPhamUseCase>();
builder.Services.AddScoped<IThemSanPhamUseCase, ThemSanPhamUseCase>();
builder.Services.AddScoped<ICapNhatSanPhamUseCase, CapNhatSanPhamUseCase>();
builder.Services.AddScoped<IDoiTrangThaiSanPhamUseCase, DoiTrangThaiSanPhamUseCase>();

builder.Services.AddScoped<IHoaDonRepository, HoaDonRepository>();
builder.Services.AddScoped<IXemDanhSachHoaDonUseCase, XemDanhSachHoaDonUseCase>();
builder.Services.AddScoped<IXemChiTietHoaDonUseCase, XemChiTietHoaDonUseCase>();
builder.Services.AddScoped<ITaoHoaDonBanChiuUseCase, TaoHoaDonBanChiuUseCase>();

builder.Services.AddScoped<IThanhToanRepository, ThanhToanRepository>();
builder.Services.AddScoped<IXemDanhSachThanhToanUseCase, XemDanhSachThanhToanUseCase>();
builder.Services.AddScoped<IXemTruocPhanBoFifoUseCase, XemTruocPhanBoFifoUseCase>();
builder.Services.AddScoped<ITaoThanhToanUseCase, TaoThanhToanUseCase>();

builder.Services.AddScoped<IBaoCaoTuoiNoRepository, BaoCaoTuoiNoRepository>();
builder.Services.AddScoped<IXemBaoCaoTuoiNoUseCase, XemBaoCaoTuoiNoUseCase>();

var app = builder.Build();

// Chỉ chạy seeder khi môi trường là Development VÀ được bật rõ ràng qua cấu hình (User Secrets hoặc biến môi trường)
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("EnableDemoSeeder", false))
{
    using var scope = app.Services.CreateScope();
    await NguoiDungSeeder.SeedFromConfigurationAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    AuthDiagnosticLogger.Initialize(app.Environment.ContentRootPath);

    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? "";
        // Bỏ qua static files, chỉ log các request trang/navigation
        if (!path.StartsWith("/_content") && !path.StartsWith("/css") && !path.Contains('.'))
        {
            var isAuth = context.User.Identity?.IsAuthenticated == true;
            var userName = isAuth ? context.User.Identity?.Name : "(chua_xac_thuc)";
            var role = isAuth ? context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value : "(khong)";
            var hasAuthCookie = context.Request.Cookies.ContainsKey("BanHangCongNo.Auth");

            AuthDiagnosticLogger.Log(
                app.Logger,
                $"[AUTH-DIAG] HTTP {context.Request.Method} {path} | CoAuthCookie={hasAuthCookie} | IsAuthenticated={isAuth} | User='{userName}' | VaiTro='{role}'");
        }
        await next();
    });
}

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public partial class Program { }
