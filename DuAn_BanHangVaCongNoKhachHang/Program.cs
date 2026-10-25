using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.Reports.Excel;
using BanHang.UseCases.BaoCaoTuoiNoScreen;
using BanHang.UseCases.Common;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.PluginInterfaces.Reports;
using BanHang.UseCases.PluginInterfaces.Security;
using BanHang.UseCases.EmailNhacNoScreen;
using BanHang.UseCases.NhatKyEmailScreen;
using BanHang.UseCases.PluginInterfaces.AI;
using BanHang.UseCases.PluginInterfaces.Email;
using BanHang.UseCases.SanPhamScreen;
using BanHang.UseCases.Security;
using BanHang.UseCases.ThanhToanScreen;
using DuAn_BanHangVaCongNoKhachHang.Data;
using DuAn_BanHangVaCongNoKhachHang.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;

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
builder.Services.AddScoped<IHoaDonNhapStateService, HoaDonNhapStateService>();

builder.Services.AddScoped<IThanhToanRepository, ThanhToanRepository>();
builder.Services.AddScoped<IXemDanhSachThanhToanUseCase, XemDanhSachThanhToanUseCase>();
builder.Services.AddScoped<IXemTruocPhanBoFifoUseCase, XemTruocPhanBoFifoUseCase>();
var vietQrSettings = builder.Configuration.GetSection("VietQrSettings").Get<VietQrSettings>() ?? new VietQrSettings();
builder.Services.AddSingleton(vietQrSettings);
builder.Services.AddScoped<IVietQrService, VietQrService>();
builder.Services.AddScoped<ITaoThanhToanUseCase, TaoThanhToanUseCase>();

builder.Services.AddScoped<IBaoCaoTuoiNoRepository, BaoCaoTuoiNoRepository>();
builder.Services.AddScoped<IXemBaoCaoTuoiNoUseCase, XemBaoCaoTuoiNoUseCase>();
builder.Services.AddScoped<IBaoCaoTuoiNoExcelExporter, ClosedXmlBaoCaoTuoiNoExcelExporter>();
builder.Services.AddScoped<IXuatBaoCaoTuoiNoExcelUseCase, XuatBaoCaoTuoiNoExcelUseCase>();

// Đăng ký dịch vụ AI (Độc lập cấu hình: Fake hoặc OpenAI/Provider thật)
var aiProvider = builder.Configuration["AiSettings:Provider"]?.Trim() ?? "Fake";
if (string.Equals(aiProvider, "Fake", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IChatClient, FakeChatClient>();
}
else if (string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(aiProvider, "Real", StringComparison.OrdinalIgnoreCase))
{
    var apiKey = builder.Configuration["AiSettings:ApiKey"]?.Trim();
    var modelId = builder.Configuration["AiSettings:ModelId"]?.Trim() ?? "gpt-4o-mini";
    var endpoint = builder.Configuration["AiSettings:Endpoint"]?.Trim();

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "Cấu hình AI nhà cung cấp 'OpenAI' đang được bật nhưng thiếu ApiKey. " +
            "Vui lòng cấu hình qua User Secrets ('dotnet user-secrets set \"AiSettings:ApiKey\" \"...\"') hoặc biến môi trường AISETTINGS__APIKEY. " +
            "Hệ thống tuyệt đối không âm thầm chuyển sang mô phỏng.");
    }

    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        OpenAI.OpenAIClient openAiClient;
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            var options = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            };
            openAiClient = new OpenAI.OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options);
        }
        else
        {
            openAiClient = new OpenAI.OpenAIClient(apiKey);
        }
        return openAiClient.GetChatClient(modelId).AsIChatClient();
    });
}
else if (string.Equals(aiProvider, "Gemini", StringComparison.OrdinalIgnoreCase))
{
    var apiKey = builder.Configuration["AiSettings:ApiKey"]?.Trim();
    var modelId = builder.Configuration["AiSettings:ModelId"]?.Trim();
    // Luôn ưu tiên cấu hình người dùng (User Secrets / appsettings / biến môi trường).
    // Chỉ áp dụng giá trị dự phòng khi ModelId để trống.
    if (string.IsNullOrWhiteSpace(modelId))
    {
        modelId = "gemini-3.6-flash"; // Mô hình Flash GA phù hợp cho project mới có Free Tier (Google AI Studio, đối chiếu ngày 21/10/2026: https://ai.google.dev/gemini-api/docs/models/gemini)
    }
    var endpoint = builder.Configuration["AiSettings:Endpoint"]?.Trim();
    if (string.IsNullOrWhiteSpace(endpoint))
    {
        endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/";
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "Cấu hình AI nhà cung cấp 'Gemini' đang được bật nhưng thiếu ApiKey. " +
            "Vui lòng lấy API key từ Google AI Studio (https://aistudio.google.com/) và cấu hình qua User Secrets " +
            "('dotnet user-secrets set \"AiSettings:ApiKey\" \"...\"') hoặc biến môi trường AISETTINGS__APIKEY. " +
            "Hệ thống tuyệt đối không âm thầm chuyển sang mô phỏng.");
    }

    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        var options = new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        };
        var geminiClient = new OpenAI.OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options);
        return geminiClient.GetChatClient(modelId).AsIChatClient();
    });
}
else
{
    throw new InvalidOperationException($"Nhà cung cấp AI '{aiProvider}' không được hỗ trợ. Các giá trị hợp lệ: 'Fake', 'OpenAI', 'Gemini'.");
}

builder.Services.AddScoped<IAiEmailDraftingService, AiChatClientEmailDraftingService>();

// Đăng ký dịch vụ Gửi Email (Độc lập cấu hình: Dev/Sandbox hoặc Real SMTP)
var emailMode = builder.Configuration["EmailSettings:Mode"]?.Trim() ?? "Dev";
if (string.Equals(emailMode, "Dev", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(emailMode, "Sandbox", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailSender, DevEmailSender>();
}
else if (string.Equals(emailMode, "Real", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(emailMode, "Smtp", StringComparison.OrdinalIgnoreCase))
{
    var smtpServer = builder.Configuration["EmailSettings:SmtpServer"]?.Trim();
    var password = builder.Configuration["EmailSettings:Password"];

    if (string.IsNullOrWhiteSpace(smtpServer))
    {
        throw new InvalidOperationException(
            "Chế độ gửi email thật ('Real/SMTP') đang được bật nhưng thiếu cấu hình máy chủ 'EmailSettings:SmtpServer'. " +
            "Hệ thống tuyệt đối không âm thầm chuyển sang mô phỏng.");
    }
    if (string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "Chế độ gửi email thật ('Real/SMTP') đang được bật nhưng thiếu mật khẩu 'EmailSettings:Password'. " +
            "Vui lòng cấu hình qua User Secrets ('dotnet user-secrets set \"EmailSettings:Password\" \"...\"') hoặc biến môi trường EMAILSETTINGS__PASSWORD. " +
            "Hệ thống tuyệt đối không âm thầm chuyển sang mô phỏng.");
    }

    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    throw new InvalidOperationException($"Chế độ gửi email '{emailMode}' không hợp lệ. Các giá trị hợp lệ: 'Dev', 'Real'.");
}

builder.Services.AddScoped<INhatKyEmailRepository, NhatKyEmailRepository>();
builder.Services.AddSingleton<IDuyetEmailStore, MemoryDuyetEmailStore>();
builder.Services.AddSingleton<IEmailIdempotencyStore, MemoryEmailIdempotencyStore>();

// UseCases soạn thảo, phê duyệt và gửi email nhắc nợ (Scoped)
builder.Services.AddScoped<IXemDuLieuNhacNoKhachHangUseCase, XemDuLieuNhacNoKhachHangUseCase>();
builder.Services.AddScoped<ISoanEmailNhacNoUseCase, SoanEmailNhacNoUseCase>();
builder.Services.AddScoped<IPheDuyetEmailNhacNoUseCase, PheDuyetEmailNhacNoUseCase>();
builder.Services.AddScoped<IGuiEmailNhacNoUseCase, GuiEmailNhacNoUseCase>();

// UseCases tra cứu và xem chi tiết nhật ký email nhắc nợ (Scoped)
builder.Services.AddScoped<IXemDanhSachNhatKyEmailUseCase, XemDanhSachNhatKyEmailUseCase>();
builder.Services.AddScoped<IXemChiTietNhatKyEmailUseCase, XemChiTietNhatKyEmailUseCase>();

var app = builder.Build();

// Chỉ chạy seeder khi môi trường là Development VÀ được bật rõ ràng qua cấu hình (User Secrets hoặc biến môi trường)
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("EnableDemoSeeder", false))
{
    using var scope = app.Services.CreateScope();
    await NguoiDungSeeder.SeedFromConfigurationAsync(scope.ServiceProvider, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    // Kiểm tra chẩn đoán trạng thái schema đối soát VietQR cho môi trường Development / Verify_K21
    try
    {
        var connStr = app.Configuration.GetConnectionString("BanHangCongNo");
        if (!string.IsNullOrWhiteSpace(connStr))
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(1) 
                FROM sys.columns 
                WHERE object_id = OBJECT_ID('dbo.ThanhToans') 
                  AND name IN ('NoiDungChuyenKhoan', 'TaiKhoanThuHuong', 'NganHangThuHuong', 'DaXacNhanTienVao', 'ThoiDiemXacNhan', 'NguoiXacNhan');";
            var colsCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (colsCount == 6)
            {
                app.Logger.LogInformation("[VIETQR-SCHEMA] Cơ sở dữ liệu đã có đầy đủ 6/6 cột audit VietQR trong dbo.ThanhToans.");
            }
            else
            {
                app.Logger.LogWarning("[VIETQR-SCHEMA] Cơ sở dữ liệu hiện có {ColsCount}/6 cột audit VietQR trong dbo.ThanhToans. Để kích hoạt tính năng Chuyển khoản VietQR, vui lòng chạy migration: 'BanHang.DataStore.SQL/Scripts/Migration_Add_VietQR_Audit_To_ThanhToans.sql'. (Phiếu thu Tiền mặt vẫn hoạt động bình thường).", colsCount);
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogDebug(ex, "[VIETQR-SCHEMA] Bỏ qua kiểm tra schema lúc khởi động: {Message}", ex.Message);
    }
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
