using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử bảo mật tầng HTTP thực tế bằng WebApplicationFactory (TestServer)
/// - Xác minh Antiforgery trên POST /login và POST /logout
/// - Xác minh GET /logout không đăng xuất
/// </summary>
public class HttpSecurityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    static HttpSecurityIntegrationTests()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__BanHangCongNo",
            "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        );
    }

    public HttpSecurityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task Test01_PostLogin_ThieuAntiforgery_BiTuChoiHttp400()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.TenDangNhap", "nvbh"),
            new KeyValuePair<string, string>("Input.MatKhau", "TestPassword")
        });

        var response = await client.PostAsync("/login", content);

        // Server ASP.NET Core từ chối request thiếu antiforgery token với mã 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Test02_PostLogout_ThieuAntiforgery_BiTuChoiHttp400()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[0]);

        var response = await client.PostAsync("/logout", content);

        // Server ASP.NET Core từ chối request thiếu antiforgery token với mã 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Test03_GetLogout_KhongThucHienDangXuat_TraVeTrangXacNhanHttp200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/logout");

        // GET /logout trả về trang xác nhận (200 OK), không phải 302 Redirect đăng xuất
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Xác nhận đăng xuất", html);
        Assert.Contains("Đăng xuất ngay", html);
    }

    [Fact]
    public async Task Test04_LoginFlow_DangNhapThanhCong_ChuyenHuongVaoHoaDon_VaTruyCapThanhCong()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. GET /login để lấy Antiforgery token và cookie
        var getLogin = await client.GetAsync("/login");
        Assert.Equal(HttpStatusCode.OK, getLogin.StatusCode);
        var html = await getLogin.Content.ReadAsStringAsync();

        var match = System.Text.RegularExpressions.Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        Assert.True(match.Success, "Không tìm thấy __RequestVerificationToken trên trang login");
        string token = match.Groups[1].Value;

        var afCookies = getLogin.Headers.Contains("Set-Cookie")
            ? string.Join("; ", getLogin.Headers.GetValues("Set-Cookie"))
            : "";

        // 2. POST /login với nvbh và mật khẩu đúng, ReturnUrl để trống hoặc /login
        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/login");
        if (!string.IsNullOrEmpty(afCookies)) postRequest.Headers.Add("Cookie", afCookies);
        postRequest.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Input.TenDangNhap", "nvbh"),
            new KeyValuePair<string, string>("Input.MatKhau", "Nvbh@2026!"),
            new KeyValuePair<string, string>("ReturnUrl", "/login") // Cố tình truyền /login
        });

        var postResponse = await client.SendAsync(postRequest);

        // 3. Phải trả về 302 Redirect đến "/hoa-don" (không bao giờ chuyển lại /login)
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal("/hoa-don", postResponse.Headers.Location?.ToString());

        // Phải có cookie Set-Cookie BanHangCongNo.Auth
        Assert.True(postResponse.Headers.Contains("Set-Cookie"), "Response không chứa Set-Cookie");
        var cookieHeader = string.Join("; ", postResponse.Headers.GetValues("Set-Cookie"));
        Assert.Contains("BanHangCongNo.Auth=", cookieHeader);

        // 4. Truy cập trang được bảo vệ /hoa-don kèm cookie
        var hoaDonRequest = new HttpRequestMessage(HttpMethod.Get, "/hoa-don");
        hoaDonRequest.Headers.Add("Cookie", cookieHeader);
        var hoaDonResponse = await client.SendAsync(hoaDonRequest);

        // Phải trả về 200 OK trực tiếp, KHÔNG redirect lặp lại về /login
        Assert.Equal(HttpStatusCode.OK, hoaDonResponse.StatusCode);
        var hoaDonHtml = await hoaDonResponse.Content.ReadAsStringAsync();
        Assert.Contains("Hóa đơn", hoaDonHtml);
        Assert.DoesNotContain("Yêu cầu đăng nhập", hoaDonHtml);
    }

    [Fact]
    public async Task Test05_LoginFlow_DangNhapSaiMatKhau_BiTuChoi_KhongCapCookieAuth()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. GET /login để lấy Antiforgery token và cookie
        var getLogin = await client.GetAsync("/login");
        var html = await getLogin.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        Assert.True(match.Success);
        string token = match.Groups[1].Value;

        var afCookies = getLogin.Headers.Contains("Set-Cookie")
            ? string.Join("; ", getLogin.Headers.GetValues("Set-Cookie"))
            : "";

        // 2. POST /login với mật khẩu sai
        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/login");
        if (!string.IsNullOrEmpty(afCookies)) postRequest.Headers.Add("Cookie", afCookies);
        postRequest.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Input.TenDangNhap", "nvbh"),
            new KeyValuePair<string, string>("Input.MatKhau", "MatKhauSaiHoanToan123!"),
            new KeyValuePair<string, string>("ReturnUrl", "/hoa-don")
        });

        var response = await client.SendAsync(postRequest);

        // 3. Phải trả về HTTP 200 (ở lại trang login) chứ không redirect
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // KHÔNG được cấp cookie đăng nhập BanHangCongNo.Auth
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            var cookieStr = string.Join("; ", setCookies);
            Assert.DoesNotContain("BanHangCongNo.Auth=", cookieStr);
        }

        // Nội dung trang phải chứa thông báo lỗi
        var responseHtml = await response.Content.ReadAsStringAsync();
        var decodedHtml = System.Net.WebUtility.HtmlDecode(responseHtml);
        Assert.Contains("Tên đăng nhập hoặc mật khẩu không chính xác.", decodedHtml);
    }

    [Fact]
    public async Task Test06_LoginFlow_DaDangNhap_VaoLaiLogin_ChuyenHuongVaoHoaDon_KhongLapTrang()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. Lấy token và đăng nhập thành công
        var getLogin = await client.GetAsync("/login");
        var html = await getLogin.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        var afCookies = getLogin.Headers.Contains("Set-Cookie")
            ? string.Join("; ", getLogin.Headers.GetValues("Set-Cookie"))
            : "";

        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/login");
        if (!string.IsNullOrEmpty(afCookies)) postRequest.Headers.Add("Cookie", afCookies);
        postRequest.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Input.TenDangNhap", "nvbh"),
            new KeyValuePair<string, string>("Input.MatKhau", "Nvbh@2026!"),
            new KeyValuePair<string, string>("ReturnUrl", "")
        });

        var postResponse = await client.SendAsync(postRequest);
        var cookieHeader = string.Join("; ", postResponse.Headers.GetValues("Set-Cookie"));

        // 2. Gửi GET /login kèm cookie đã đăng nhập
        var loginAgainRequest = new HttpRequestMessage(HttpMethod.Get, "/login");
        loginAgainRequest.Headers.Add("Cookie", cookieHeader);
        var loginAgainResponse = await client.SendAsync(loginAgainRequest);

        // Phải chuyển hướng ngay đến "/hoa-don", không chuyển hướng lại "/login"
        Assert.Equal(HttpStatusCode.Redirect, loginAgainResponse.StatusCode);
        Assert.Equal("/hoa-don", loginAgainResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Test07_HoaDonList_Render_Va_ChiTietHoaDon_HttpContent()
    {
        // 0. Đảm bảo dữ liệu HD002 và HD004 có sẵn trên database kiểm thử QuanLyBanHangCongNo_Test
        int idHd002 = 0;
        int idHd004 = 0;
        using (var conn = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"))
        {
            await conn.OpenAsync();

            var khId = await conn.ExecuteScalarAsync<int?>(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.KhachHangs WHERE MaKhachHang = 'KH_TEST_HTTP')
                BEGIN
                    INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                    VALUES ('KH_TEST_HTTP', N'Khách Hàng HTTP Test', 50000000, 1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    SELECT KhachHangId FROM dbo.KhachHangs WHERE MaKhachHang = 'KH_TEST_HTTP';
                END");

            var spId = await conn.ExecuteScalarAsync<int?>(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.SanPhams WHERE MaSanPham = 'SP_TEST_HTTP')
                BEGIN
                    INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                    VALUES ('SP_TEST_HTTP', N'Sản phẩm HTTP Test', N'Cái', 1000000, 100, 1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    SELECT SanPhamId FROM dbo.SanPhams WHERE MaSanPham = 'SP_TEST_HTTP';
                END");

            var existingHd2 = await conn.ExecuteScalarAsync<int?>("SELECT HoaDonId FROM dbo.HoaDons WHERE MaHoaDon = 'HD002'");
            if (existingHd2.HasValue)
            {
                idHd002 = existingHd2.Value;
            }
            else
            {
                idHd002 = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu)
                    VALUES ('HD002', @KhId, '2026-10-01', '2026-10-31', 6800000, 6800000, 2, N'Hóa đơn test HD002');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhId = khId.Value });

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                    VALUES (@Id, @SpId, N'Màn hình Dell UltraSharp', N'Cái', 1, 6800000);", new { Id = idHd002, SpId = spId.Value });
            }

            var existingHd4 = await conn.ExecuteScalarAsync<int?>("SELECT HoaDonId FROM dbo.HoaDons WHERE MaHoaDon = 'HD004'");
            if (existingHd4.HasValue)
            {
                idHd004 = existingHd4.Value;
            }
            else
            {
                idHd004 = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu)
                    VALUES ('HD004', @KhId, '2026-10-01', '2026-10-31', 9700000, 500000, 1, N'Hóa đơn test HD004');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhId = khId.Value });

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                    VALUES (@Id, @SpId, N'Màn hình Dell UltraSharp', N'Cái', 1, 9700000);", new { Id = idHd004, SpId = spId.Value });
            }
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. Lấy Antiforgery token và đăng nhập với tài khoản ktcn
        var getLogin = await client.GetAsync("/login");
        var htmlLogin = await getLogin.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(htmlLogin, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        var afCookies = getLogin.Headers.Contains("Set-Cookie")
            ? string.Join("; ", getLogin.Headers.GetValues("Set-Cookie"))
            : "";

        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/login");
        if (!string.IsNullOrEmpty(afCookies)) postRequest.Headers.Add("Cookie", afCookies);
        postRequest.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Input.TenDangNhap", "ktcn"),
            new KeyValuePair<string, string>("Input.MatKhau", "Ktcn@2026!"),
            new KeyValuePair<string, string>("ReturnUrl", "/hoa-don")
        });

        var postResponse = await client.SendAsync(postRequest);
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        var cookieHeader = string.Join("; ", postResponse.Headers.GetValues("Set-Cookie"));

        // 2. GET /hoa-don để lấy HTML danh sách hóa đơn và kiểm tra render liên kết
        var listReq = new HttpRequestMessage(HttpMethod.Get, "/hoa-don");
        listReq.Headers.Add("Cookie", cookieHeader);
        var listResp = await client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var listHtml = await listResp.Content.ReadAsStringAsync();

        // Kiểm tra phần tử điều hướng cho HD002 và HD004:
        // Sử dụng anchor HTML độc lập với circuit: có href hợp lệ và target="_top"
        Assert.Contains($"href=\"/hoa-don/{idHd002}\"", listHtml);
        Assert.Contains($"id=\"lnk-hoadon-{idHd002}\"", listHtml);
        Assert.Contains("target=\"_top\"", listHtml);
        Assert.Contains("HD002", listHtml);
        Assert.Contains($"href=\"/hoa-don/{idHd004}\"", listHtml);
        Assert.Contains($"id=\"lnk-hoadon-{idHd004}\"", listHtml);
        Assert.Contains("HD004", listHtml);
        Assert.DoesNotContain("href=\"#\"", listHtml);
        Assert.DoesNotContain("href=\"/hoa-don/0\"", listHtml);

        // 3. GET /hoa-don/{idHd002} để kiểm tra truy cập URL chi tiết HD002
        var detail2Req = new HttpRequestMessage(HttpMethod.Get, $"/hoa-don/{idHd002}");
        detail2Req.Headers.Add("Cookie", cookieHeader);
        var detail2Resp = await client.SendAsync(detail2Req);
        Assert.Equal(HttpStatusCode.OK, detail2Resp.StatusCode);
        var detail2Html = await detail2Resp.Content.ReadAsStringAsync();

        // Xác minh nội dung chi tiết HD002 thực tế, không phải fallback hay lỗi quyền
        Assert.Contains("Chi tiết hóa đơn", detail2Html);
        Assert.Contains("HD002", detail2Html);
        Assert.Contains("6.800.000", detail2Html);
        Assert.DoesNotContain("404 - Không tìm thấy trang", detail2Html);
        Assert.DoesNotContain("Không có quyền truy cập", detail2Html);
        Assert.DoesNotContain("Yêu cầu đăng nhập", detail2Html);

        // 4. GET /hoa-don/{idHd004} để kiểm tra truy cập URL chi tiết HD004
        var detail4Req = new HttpRequestMessage(HttpMethod.Get, $"/hoa-don/{idHd004}");
        detail4Req.Headers.Add("Cookie", cookieHeader);
        var detail4Resp = await client.SendAsync(detail4Req);
        Assert.Equal(HttpStatusCode.OK, detail4Resp.StatusCode);
        var detail4Html = await detail4Resp.Content.ReadAsStringAsync();

        // Xác minh nội dung chi tiết HD004 thực tế
        Assert.Contains("Chi tiết hóa đơn", detail4Html);
        Assert.Contains("HD004", detail4Html);
        Assert.Contains("9.700.000", detail4Html);
        Assert.DoesNotContain("404 - Không tìm thấy trang", detail4Html);
        Assert.DoesNotContain("Không có quyền truy cập", detail4Html);
        Assert.DoesNotContain("Yêu cầu đăng nhập", detail4Html);
    }
}
