using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.SanPhamScreen;
using BanHang.UseCases.ThanhToanScreen;
using BanHang.UseCases.BaoCaoTuoiNoScreen;
using DuAn_BanHangVaCongNoKhachHang.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

// Đăng ký SQL Connection Factory từ cấu hình
var connectionString = builder.Configuration.GetConnectionString("BanHangCongNo")
    ?? throw new InvalidOperationException("Chuỗi kết nối 'BanHangCongNo' không tìm thấy trong cấu hình.");
builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));

// Đăng ký Repository và Use Cases (Scoped)
builder.Services.AddScoped<IKhachHangRepository, KhachHangRepository>();
builder.Services.AddScoped<IXemDanhSachKhachHangUseCase, XemDanhSachKhachHangUseCase>();
builder.Services.AddScoped<IThemKhachHangUseCase, ThemKhachHangUseCase>();
builder.Services.AddScoped<ICapNhatKhachHangUseCase, CapNhatKhachHangUseCase>();
builder.Services.AddScoped<IDoiTrangThaiKhachHangUseCase, DoiTrangThaiKhachHangUseCase>();

builder.Services.AddScoped<ISanPhamRepository, SanPhamRepository>();
builder.Services.AddScoped<IXemDanhSachSanPhamHoatDongUseCase, XemDanhSachSanPhamHoatDongUseCase>();

builder.Services.AddScoped<IHoaDonRepository, HoaDonRepository>();
builder.Services.AddScoped<IXemDanhSachHoaDonUseCase, XemDanhSachHoaDonUseCase>();
builder.Services.AddScoped<ITaoHoaDonBanChiuUseCase, TaoHoaDonBanChiuUseCase>();

builder.Services.AddScoped<IThanhToanRepository, ThanhToanRepository>();
builder.Services.AddScoped<IXemDanhSachThanhToanUseCase, XemDanhSachThanhToanUseCase>();
builder.Services.AddScoped<IXemTruocPhanBoFifoUseCase, XemTruocPhanBoFifoUseCase>();
builder.Services.AddScoped<ITaoThanhToanUseCase, TaoThanhToanUseCase>();

builder.Services.AddScoped<IBaoCaoTuoiNoRepository, BaoCaoTuoiNoRepository>();
builder.Services.AddScoped<IXemBaoCaoTuoiNoUseCase, XemBaoCaoTuoiNoUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
