using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.BaoCaoTuoiNoScreen;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.PluginInterfaces.Security;
using BanHang.UseCases.SanPhamScreen;
using BanHang.UseCases.Security;
using BanHang.UseCases.ThanhToanScreen;
using DuAn_BanHangVaCongNoKhachHang.Services;
using Xunit;

namespace BanHang.Tests;

public class AuthUnitTests
{
    private class FakeNguoiDungRepository : INguoiDungRepository
    {
        private readonly Dictionary<string, NguoiDung> _users = new(StringComparer.OrdinalIgnoreCase);

        public void AddUser(NguoiDung user)
        {
            _users[user.TenDangNhap] = user;
        }

        public Task<NguoiDung?> LayTheoTenDangNhapAsync(string tenDangNhap)
        {
            _users.TryGetValue(tenDangNhap, out var user);
            return Task.FromResult<NguoiDung?>(user);
        }

        public Task<bool> TonTaiTenDangNhapAsync(string tenDangNhap)
        {
            return Task.FromResult(_users.ContainsKey(tenDangNhap));
        }

        public Task<int> TaoNguoiDungAsync(NguoiDung nguoiDung)
        {
            _users[nguoiDung.TenDangNhap] = nguoiDung;
            return Task.FromResult(1);
        }

        public Task<int> DemSoLuongNguoiDungAsync()
        {
            return Task.FromResult(_users.Count);
        }
    }

    private readonly IPasswordHasherService _hasher = new PasswordHasherService();
    private readonly FakeNguoiDungRepository _userRepo = new();
    private readonly ILoginRateLimiter _rateLimiter = new MemoryLoginRateLimiter();
    private readonly DangNhapUseCase _dangNhapUseCase;

    public AuthUnitTests()
    {
        _dangNhapUseCase = new DangNhapUseCase(_userRepo, _hasher, _rateLimiter);

        // Khởi tạo tài khoản test
        _userRepo.AddUser(new NguoiDung
        {
            NguoiDungId = 1,
            TenDangNhap = "nvbh",
            MatKhauHash = _hasher.HashPassword("MatKhauDung123!"),
            HoTen = "Nhân Viên Bán Hàng",
            VaiTro = VaiTroConstants.NhanVienBanHang,
            TrangThai = true,
            NgayTao = DateTime.Now
        });

        _userRepo.AddUser(new NguoiDung
        {
            NguoiDungId = 2,
            TenDangNhap = "ktcn",
            MatKhauHash = _hasher.HashPassword("MatKhauKeToan456!"),
            HoTen = "Kế Toán Công Nợ",
            VaiTro = VaiTroConstants.KeToanCongNo,
            TrangThai = true,
            NgayTao = DateTime.Now
        });

        _userRepo.AddUser(new NguoiDung
        {
            NguoiDungId = 3,
            TenDangNhap = "user_khoa",
            MatKhauHash = _hasher.HashPassword("MatKhauKhoa789!"),
            HoTen = "Tài Khoản Bị Khóa",
            VaiTro = VaiTroConstants.NhanVienBanHang,
            TrangThai = false, // Bị khóa
            NgayTao = DateTime.Now
        });
    }

    [Fact]
    public async Task Test01_DangNhapDungMatKhau_ThanhCong()
    {
        var result = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = "nvbh",
            MatKhau = "MatKhauDung123!"
        });

        Assert.True(result.ThanhCong);
        Assert.NotNull(result.NguoiDung);
        Assert.Equal("nvbh", result.NguoiDung.TenDangNhap);
        Assert.Equal(VaiTroConstants.NhanVienBanHang, result.NguoiDung.VaiTro);
    }

    [Fact]
    public async Task Test02_DangNhapSaiMatKhau_TraThongBaoChung()
    {
        var result = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = "nvbh",
            MatKhau = "MatKhauSai"
        });

        Assert.False(result.ThanhCong);
        Assert.Null(result.NguoiDung);
        Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", result.ThongBao);
    }

    [Fact]
    public async Task Test03_DangNhapTaiKhoanKhongTonTai_TraThongBaoChung()
    {
        var result = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = "khong_ton_tai",
            MatKhau = "MatKhauBatKy"
        });

        Assert.False(result.ThanhCong);
        Assert.Null(result.NguoiDung);
        Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", result.ThongBao);
    }

    [Fact]
    public async Task Test04_DangNhapTaiKhoanBiKhoa_TraThongBaoChung()
    {
        var result = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = "user_khoa",
            MatKhau = "MatKhauKhoa789!"
        });

        Assert.False(result.ThanhCong);
        Assert.Null(result.NguoiDung);
        Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác.", result.ThongBao);
    }

    [Fact]
    public async Task Test05_GioiHanThuSai_LockoutSau5Lan()
    {
        string username = "test_lockout_user";
        _userRepo.AddUser(new NguoiDung
        {
            NguoiDungId = 99,
            TenDangNhap = username,
            MatKhauHash = _hasher.HashPassword("MatKhauChuan"),
            HoTen = "Test Lockout",
            VaiTro = VaiTroConstants.KeToanCongNo,
            TrangThai = true
        });

        // Thử sai 5 lần
        for (int i = 1; i <= 5; i++)
        {
            var res = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
            {
                TenDangNhap = username,
                MatKhau = "Sai" + i
            });
            Assert.False(res.ThanhCong);
            Assert.False(res.BiKhoaDoThuSai);
        }

        // Lần thứ 6: Bị khóa do thử sai quá nhiều
        var lockoutRes = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = username,
            MatKhau = "MatKhauChuan" // Dù nhập đúng cũng bị chặn
        });

        Assert.False(lockoutRes.ThanhCong);
        Assert.True(lockoutRes.BiKhoaDoThuSai);
        Assert.Contains("thử lại sau 15 phút", lockoutRes.ThongBao);

        // Reset sau khi hết hạn hoặc mở khóa
        _rateLimiter.ResetAttempts(username);

        // Đăng nhập lại với mật khẩu đúng
        var successRes = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = username,
            MatKhau = "MatKhauChuan"
        });
        Assert.True(successRes.ThanhCong);
    }

    [Fact]
    public void Test06_PasswordHasher_KhongLuuMatKhauRo_BaoMatChuanIdentity()
    {
        string rawPass = "MySecurePassword#2026";
        string hash = _hasher.HashPassword(rawPass);

        Assert.NotEqual(rawPass, hash);
        Assert.True(_hasher.VerifyPassword(hash, rawPass));
        Assert.False(_hasher.VerifyPassword(hash, "WrongPassword"));
    }

    // =========================================================================
    // PHÂN QUYỀN TRÊN TỪNG USECASE (ANONYMOUS, NHANVIENBANHANG, KETOANCONGNO)
    // =========================================================================

    [Fact]
    public async Task Test07_Anonymous_BiTuChoiTatCaUseCases()
    {
        var anonymous = FakeCurrentUserService.Anonymous();

        // 1. Khách Hàng
        var xemKhUseCase = new XemDanhSachKhachHangUseCase(new FakeKhachHangRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemKhUseCase.ExecuteAsync());

        var themKhUseCase = new ThemKhachHangUseCase(new FakeKhachHangRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => themKhUseCase.ExecuteAsync(new TaoKhachHangRequest()));

        var capNhatKhUseCase = new CapNhatKhachHangUseCase(new FakeKhachHangRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => capNhatKhUseCase.ExecuteAsync(new CapNhatKhachHangRequest()));

        var doiTtKhUseCase = new DoiTrangThaiKhachHangUseCase(new FakeKhachHangRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => doiTtKhUseCase.ExecuteAsync(1, false));

        // 2. Sản Phẩm
        var xemSpHdUseCase = new XemDanhSachSanPhamHoatDongUseCase(new FakeSanPhamRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemSpHdUseCase.ExecuteAsync());

        var xemSpTatCaUseCase = new XemDanhSachSanPhamUseCase(new FakeSanPhamRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemSpTatCaUseCase.ExecuteAsync());

        var themSpUseCase = new ThemSanPhamUseCase(new FakeSanPhamRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => themSpUseCase.ExecuteAsync(new TaoSanPhamRequest()));

        var capNhatSpUseCase = new CapNhatSanPhamUseCase(new FakeSanPhamRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => capNhatSpUseCase.ExecuteAsync(new CapNhatSanPhamRequest()));

        var doiTtSpUseCase = new DoiTrangThaiSanPhamUseCase(new FakeSanPhamRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => doiTtSpUseCase.ExecuteAsync(1, false));

        // 3. Hóa Đơn
        var xemHdUseCase = new XemDanhSachHoaDonUseCase(new FakeHoaDonRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemHdUseCase.ExecuteAsync());

        var taoHdUseCase = new TaoHoaDonBanChiuUseCase(new FakeHoaDonRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => taoHdUseCase.ExecuteAsync(new TaoHoaDonRequest()));

        // 4. Thanh Toán & Tuổi Nợ
        var xemTtUseCase = new XemDanhSachThanhToanUseCase(new FakeThanhToanRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemTtUseCase.ExecuteAsync());

        var xemTruocFifoUseCase = new XemTruocPhanBoFifoUseCase(new FakeThanhToanRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemTruocFifoUseCase.ExecuteAsync(new XemTruocPhanBoRequest()));

        var taoTtUseCase = new TaoThanhToanUseCase(new FakeThanhToanRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => taoTtUseCase.ExecuteAsync(new TaoThanhToanRequest()));

        var xemBaoCaoTuoiNoUseCase = new XemBaoCaoTuoiNoUseCase(new FakeBaoCaoTuoiNoRepoMock(), anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemBaoCaoTuoiNoUseCase.ExecuteAsync(DateTime.Today));
    }

    [Fact]
    public async Task Test08_NhanVienBanHang_ChiDuocPhepXemHoaDon_TaoHoaDon_DocKhachHangVaSanPhamChoForm()
    {
        var nvbh = FakeCurrentUserService.NhanVienBanHang();

        // Được phép:
        var xemHdUseCase = new XemDanhSachHoaDonUseCase(new FakeHoaDonRepoMock(), nvbh);
        await xemHdUseCase.ExecuteAsync(); // Không ném UnauthorizedAccessException

        var xemKhUseCase = new XemDanhSachKhachHangUseCase(new FakeKhachHangRepoMock(), nvbh);
        await xemKhUseCase.ExecuteAsync(); // Không ném

        var xemSpHdUseCase = new XemDanhSachSanPhamHoatDongUseCase(new FakeSanPhamRepoMock(), nvbh);
        await xemSpHdUseCase.ExecuteAsync(); // Không ném

        var taoHdUseCase = new TaoHoaDonBanChiuUseCase(new FakeHoaDonRepoMock(), nvbh);
        var hdResult = await taoHdUseCase.ExecuteAsync(new TaoHoaDonRequest()); // Pass auth check (fails later on validation if empty)
        Assert.False(hdResult.ThanhCong); // Bị từ chối do validation nghiệp vụ, không phải do authorization

        // BỊ TỪ CHỐI (các thao tác quản lý khách hàng, sản phẩm, phiếu thu, tuổi nợ):
        var themKhUseCase = new ThemKhachHangUseCase(new FakeKhachHangRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => themKhUseCase.ExecuteAsync(new TaoKhachHangRequest()));

        var capNhatKhUseCase = new CapNhatKhachHangUseCase(new FakeKhachHangRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => capNhatKhUseCase.ExecuteAsync(new CapNhatKhachHangRequest()));

        var doiTtKhUseCase = new DoiTrangThaiKhachHangUseCase(new FakeKhachHangRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => doiTtKhUseCase.ExecuteAsync(1, false));

        var xemSpTatCaUseCase = new XemDanhSachSanPhamUseCase(new FakeSanPhamRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemSpTatCaUseCase.ExecuteAsync());

        var themSpUseCase = new ThemSanPhamUseCase(new FakeSanPhamRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => themSpUseCase.ExecuteAsync(new TaoSanPhamRequest()));

        var capNhatSpUseCase = new CapNhatSanPhamUseCase(new FakeSanPhamRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => capNhatSpUseCase.ExecuteAsync(new CapNhatSanPhamRequest()));

        var doiTtSpUseCase = new DoiTrangThaiSanPhamUseCase(new FakeSanPhamRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => doiTtSpUseCase.ExecuteAsync(1, false));

        var xemTtUseCase = new XemDanhSachThanhToanUseCase(new FakeThanhToanRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemTtUseCase.ExecuteAsync());

        var xemTruocFifoUseCase = new XemTruocPhanBoFifoUseCase(new FakeThanhToanRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemTruocFifoUseCase.ExecuteAsync(new XemTruocPhanBoRequest()));

        var taoTtUseCase = new TaoThanhToanUseCase(new FakeThanhToanRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => taoTtUseCase.ExecuteAsync(new TaoThanhToanRequest()));

        var xemBaoCaoTuoiNoUseCase = new XemBaoCaoTuoiNoUseCase(new FakeBaoCaoTuoiNoRepoMock(), nvbh);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => xemBaoCaoTuoiNoUseCase.ExecuteAsync(DateTime.Today));
    }

    [Fact]
    public async Task Test09_KeToanCongNo_QuanLyKhachHang_SanPham_PhieuThu_TuoiNo_KhongDuocTaoHoaDon()
    {
        var ktcn = FakeCurrentUserService.KeToanCongNo();

        // Được phép:
        var xemKhUseCase = new XemDanhSachKhachHangUseCase(new FakeKhachHangRepoMock(), ktcn);
        await xemKhUseCase.ExecuteAsync();

        var xemSpTatCaUseCase = new XemDanhSachSanPhamUseCase(new FakeSanPhamRepoMock(), ktcn);
        await xemSpTatCaUseCase.ExecuteAsync();

        var xemSpHdUseCase = new XemDanhSachSanPhamHoatDongUseCase(new FakeSanPhamRepoMock(), ktcn);
        await xemSpHdUseCase.ExecuteAsync();

        var xemHdUseCase = new XemDanhSachHoaDonUseCase(new FakeHoaDonRepoMock(), ktcn);
        await xemHdUseCase.ExecuteAsync();

        var xemTtUseCase = new XemDanhSachThanhToanUseCase(new FakeThanhToanRepoMock(), ktcn);
        await xemTtUseCase.ExecuteAsync();

        var xemBaoCaoTuoiNoUseCase = new XemBaoCaoTuoiNoUseCase(new FakeBaoCaoTuoiNoRepoMock(), ktcn);
        await xemBaoCaoTuoiNoUseCase.ExecuteAsync(DateTime.Today);

        // BỊ TỪ CHỐI: Tạo hóa đơn bán chịu (chức năng thuộc nhân viên bán hàng)
        var taoHdUseCase = new TaoHoaDonBanChiuUseCase(new FakeHoaDonRepoMock(), ktcn);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => taoHdUseCase.ExecuteAsync(new TaoHoaDonRequest()));
    }

    [Theory]
    [InlineData("https://malicious.com", "/hoa-don")]
    [InlineData("http://attacker.com/steal", "/hoa-don")]
    [InlineData("//evil.com", "/hoa-don")]
    [InlineData("", "/hoa-don")]
    [InlineData(null, "/hoa-don")]
    [InlineData("/login", "/hoa-don")]
    [InlineData("/logout", "/hoa-don")]
    [InlineData("/", "/hoa-don")]
    [InlineData("/hoa-don", "/hoa-don")]
    [InlineData("/khach-hang", "/khach-hang")]
    public void Test10_OpenRedirect_ChuanHoaVeNoiBo(string? inputUrl, string expectedUrl)
    {
        var fakeUrlHelper = new FakeUrlHelper();
        string normalized = DuAn_BanHangVaCongNoKhachHang.Pages.LoginModel.NormalizeReturnUrl(fakeUrlHelper, inputUrl);
        Assert.Equal(expectedUrl, normalized);
    }

    [Fact]
    public void Test11_Antiforgery_Va_PhuongThucPost_ChoLoginVaLogout()
    {
        // 1. LoginModel có ValidateAntiForgeryToken
        var loginAttrs = typeof(DuAn_BanHangVaCongNoKhachHang.Pages.LoginModel)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);
        Assert.NotEmpty(loginAttrs);

        // 2. LogoutModel có ValidateAntiForgeryToken
        var logoutAttrs = typeof(DuAn_BanHangVaCongNoKhachHang.Pages.LogoutModel)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);
        Assert.NotEmpty(logoutAttrs);

        // 3. GET /logout chỉ hiển thị trang xác nhận (PageResult), không thực hiện SignOut
        var logoutModel = new DuAn_BanHangVaCongNoKhachHang.Pages.LogoutModel();
        var getResult = logoutModel.OnGet();
        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(getResult);
    }

    [Fact]
    public async Task Test12_BlazorCurrentUserService_RevalidationVaHetHanPhiên()
    {
        // 1. Tạo mock AuthenticationStateProvider với user ktcn có claim AuthExpiresUtc còn hạn
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, "ktcn"),
            new(System.Security.Claims.ClaimTypes.Role, VaiTroConstants.KeToanCongNo),
            new("AuthExpiresUtc", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds().ToString())
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var authProvider = new FakeAuthenticationStateProvider(principal);

        var repo = new FakeNguoiDungRepository();
        repo.AddUser(new NguoiDung
        {
            TenDangNhap = "ktcn",
            VaiTro = VaiTroConstants.KeToanCongNo,
            TrangThai = true
        });

        var currentUserService = new BlazorCurrentUserService(authProvider, repo);

        // Hợp lệ:
        Assert.True(await currentUserService.IsAuthenticatedAsync());
        Assert.True(await currentUserService.IsInRoleAsync(VaiTroConstants.KeToanCongNo));
        Assert.False(await currentUserService.IsInRoleAsync(VaiTroConstants.NhanVienBanHang));

        // 2. Tài khoản bị khóa trong DB -> Ngay lập tức mất quyền trong circuit
        repo.AddUser(new NguoiDung
        {
            TenDangNhap = "ktcn",
            VaiTro = VaiTroConstants.KeToanCongNo,
            TrangThai = false // ĐÃ KHÓA TRONG DB
        });

        Assert.False(await currentUserService.IsAuthenticatedAsync());
        Assert.False(await currentUserService.IsInRoleAsync(VaiTroConstants.KeToanCongNo));

        // 3. Mở khóa nhưng đổi vai trò thành NhanVienBanHang trong DB -> Mất quyền KeToanCongNo
        repo.AddUser(new NguoiDung
        {
            TenDangNhap = "ktcn",
            VaiTro = VaiTroConstants.NhanVienBanHang, // ĐỔI VAI TRÒ
            TrangThai = true
        });

        Assert.True(await currentUserService.IsAuthenticatedAsync());
        Assert.False(await currentUserService.IsInRoleAsync(VaiTroConstants.KeToanCongNo));
        Assert.True(await currentUserService.IsInRoleAsync(VaiTroConstants.NhanVienBanHang));

        // 4. Phiên hết hạn (AuthExpiresUtc quá khứ) -> Mất quyền
        var expiredClaims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, "ktcn"),
            new(System.Security.Claims.ClaimTypes.Role, VaiTroConstants.NhanVienBanHang),
            new("AuthExpiresUtc", DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString())
        };
        var expiredProvider = new FakeAuthenticationStateProvider(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(expiredClaims, "TestAuth")));
        var expiredUserService = new BlazorCurrentUserService(expiredProvider, repo);

        Assert.False(await expiredUserService.IsAuthenticatedAsync());
        Assert.False(await expiredUserService.IsInRoleAsync(VaiTroConstants.NhanVienBanHang));
    }

    [Fact]
    public async Task Test13_BlazorCurrentUserService_LoiDatabase_XuLyAnToanFailClosed()
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, "ktcn"),
            new(System.Security.Claims.ClaimTypes.Role, VaiTroConstants.KeToanCongNo),
            new("AuthExpiresUtc", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds().ToString())
        };
        var authProvider = new FakeAuthenticationStateProvider(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "TestAuth")));

        // Repository ném ngoại lệ mô phỏng DB chết/lỗi kết nối
        var faultyRepo = new FaultyNguoiDungRepository();
        var currentUserService = new BlazorCurrentUserService(authProvider, faultyRepo);

        // Fail-closed: từ chối quyền, không cấp phép trái phép
        Assert.False(await currentUserService.IsAuthenticatedAsync());
        Assert.False(await currentUserService.IsInRoleAsync(VaiTroConstants.KeToanCongNo));
    }

    [Fact]
    public async Task Test14_BlazorCurrentUserService_AuthExpiresUtc_BatBuocVaFailClosed()
    {
        var repo = new FakeNguoiDungRepository();
        repo.AddUser(new NguoiDung
        {
            TenDangNhap = "ktcn",
            VaiTro = VaiTroConstants.KeToanCongNo,
            TrangThai = true
        });

        // 1. Trường hợp 1: Thiếu claim AuthExpiresUtc -> Từ chối xác thực (fail-closed)
        var claimsThieu = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, "ktcn"),
            new(System.Security.Claims.ClaimTypes.Role, VaiTroConstants.KeToanCongNo)
        };
        var providerThieu = new FakeAuthenticationStateProvider(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claimsThieu, "TestAuth")));
        var serviceThieu = new BlazorCurrentUserService(providerThieu, repo);

        Assert.False(await serviceThieu.IsAuthenticatedAsync());
        Assert.False(await serviceThieu.IsInRoleAsync(VaiTroConstants.KeToanCongNo));
        Assert.Null(await serviceThieu.GetTenDangNhapAsync());

        // 2. Trường hợp 2: Claim AuthExpiresUtc sai định dạng -> Từ chối xác thực (fail-closed)
        var claimsSaiDinhDang = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, "ktcn"),
            new(System.Security.Claims.ClaimTypes.Role, VaiTroConstants.KeToanCongNo),
            new("AuthExpiresUtc", "dinh-dang-sai-khong-phai-ngay-thang")
        };
        var providerSai = new FakeAuthenticationStateProvider(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claimsSaiDinhDang, "TestAuth")));
        var serviceSai = new BlazorCurrentUserService(providerSai, repo);

        Assert.False(await serviceSai.IsAuthenticatedAsync());
        Assert.False(await serviceSai.IsInRoleAsync(VaiTroConstants.KeToanCongNo));
        Assert.Null(await serviceSai.GetTenDangNhapAsync());

        // 3. Trường hợp 3: Claim AuthExpiresUtc đã hết hạn -> Từ chối xác thực (fail-closed)
        var claimsHetHan = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, "ktcn"),
            new(System.Security.Claims.ClaimTypes.Role, VaiTroConstants.KeToanCongNo),
            new("AuthExpiresUtc", DateTimeOffset.UtcNow.AddSeconds(-30).ToUnixTimeSeconds().ToString())
        };
        var providerHetHan = new FakeAuthenticationStateProvider(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claimsHetHan, "TestAuth")));
        var serviceHetHan = new BlazorCurrentUserService(providerHetHan, repo);

        Assert.False(await serviceHetHan.IsAuthenticatedAsync());
        Assert.False(await serviceHetHan.IsInRoleAsync(VaiTroConstants.KeToanCongNo));
        Assert.Null(await serviceHetHan.GetTenDangNhapAsync());

        // 4. Đối chứng: Có claim AuthExpiresUtc hợp lệ (còn hạn) -> Xác thực thành công
        var claimsHopLe = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, "ktcn"),
            new(System.Security.Claims.ClaimTypes.Role, VaiTroConstants.KeToanCongNo),
            new("AuthExpiresUtc", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds().ToString())
        };
        var providerHopLe = new FakeAuthenticationStateProvider(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claimsHopLe, "TestAuth")));
        var serviceHopLe = new BlazorCurrentUserService(providerHopLe, repo);

        Assert.True(await serviceHopLe.IsAuthenticatedAsync());
        Assert.True(await serviceHopLe.IsInRoleAsync(VaiTroConstants.KeToanCongNo));
        Assert.Equal("ktcn", await serviceHopLe.GetTenDangNhapAsync());
    }

    private class FakeAuthenticationStateProvider : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
    {
        private readonly System.Security.Claims.ClaimsPrincipal _user;
        public FakeAuthenticationStateProvider(System.Security.Claims.ClaimsPrincipal user) => _user = user;
        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(_user));
    }

    private class FaultyNguoiDungRepository : INguoiDungRepository
    {
        public Task<NguoiDung?> LayTheoTenDangNhapAsync(string tenDangNhap) => throw new System.IO.IOException("Lỗi kết nối cơ sở dữ liệu");
        public Task<bool> TonTaiTenDangNhapAsync(string tenDangNhap) => throw new System.IO.IOException("Lỗi kết nối cơ sở dữ liệu");
        public Task<int> TaoNguoiDungAsync(NguoiDung nguoiDung) => throw new System.IO.IOException("Lỗi kết nối cơ sở dữ liệu");
        public Task<int> DemSoLuongNguoiDungAsync() => throw new System.IO.IOException("Lỗi kết nối cơ sở dữ liệu");
    }

    private class FakeUrlHelper : Microsoft.AspNetCore.Mvc.IUrlHelper
    {
        public Microsoft.AspNetCore.Mvc.ActionContext ActionContext => null!;
        public string? Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext actionContext) => null;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("/") && !url.StartsWith("//") && !url.StartsWith("/\\");
        }
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext) => null;
    }

    // Mocks hỗ trợ kiểm tra quyền
    private class FakeKhachHangRepoMock : IKhachHangRepository
    {
        public Task<IEnumerable<KhachHang>> LayDanhSachKhachHangAsync() => Task.FromResult(Enumerable.Empty<KhachHang>());
        public Task<KhachHang?> LayKhachHangTheoIdAsync(int khachHangId) => Task.FromResult<KhachHang?>(null);
        public Task<bool> KiemTraTrungMaKhachHangAsync(string maKhachHang, int? loaiTruKhachHangId = null) => Task.FromResult(false);
        public Task<decimal> LayCongNoHienTaiCuaKhachHangAsync(int khachHangId) => Task.FromResult(0m);
        public Task<TaoKhachHangResult> ThemKhachHangAsync(TaoKhachHangRequest request) => Task.FromResult(new TaoKhachHangResult { ThanhCong = true });
        public Task<CapNhatKhachHangResult> CapNhatKhachHangAsync(CapNhatKhachHangRequest request) => Task.FromResult(new CapNhatKhachHangResult { ThanhCong = true });
        public Task<DoiTrangThaiKhachHangResult> DoiTrangThaiKhachHangAsync(int khachHangId, bool trangThaiMoi) => Task.FromResult(new DoiTrangThaiKhachHangResult { ThanhCong = true });
    }

    private class FakeSanPhamRepoMock : ISanPhamRepository
    {
        public Task<IEnumerable<SanPham>> LayDanhSachSanPhamHoatDongAsync() => Task.FromResult(Enumerable.Empty<SanPham>());
        public Task<IEnumerable<SanPham>> LayDanhSachTatCaSanPhamAsync() => Task.FromResult(Enumerable.Empty<SanPham>());
        public Task<SanPham?> LaySanPhamTheoIdAsync(int sanPhamId) => Task.FromResult<SanPham?>(null);
        public Task<bool> KiemTraTrungMaSanPhamAsync(string maSanPham) => Task.FromResult(false);
        public Task<TaoSanPhamResult> ThemSanPhamAsync(TaoSanPhamRequest request) => Task.FromResult(new TaoSanPhamResult { ThanhCong = true });
        public Task<CapNhatSanPhamResult> CapNhatSanPhamAsync(CapNhatSanPhamRequest request) => Task.FromResult(new CapNhatSanPhamResult { ThanhCong = true });
        public Task<DoiTrangThaiSanPhamResult> DoiTrangThaiSanPhamAsync(int sanPhamId, bool trangThaiMoi) => Task.FromResult(new DoiTrangThaiSanPhamResult { ThanhCong = true });
    }

    private class FakeHoaDonRepoMock : IHoaDonRepository
    {
        public Task<IEnumerable<HoaDonListItemDto>> LayDanhSachHoaDonAsync() => Task.FromResult(Enumerable.Empty<HoaDonListItemDto>());
        public Task<TaoHoaDonResult> TaoHoaDonBanChiuAsync(TaoHoaDonRequest request) => Task.FromResult(new TaoHoaDonResult { ThanhCong = true });
        public Task<ChiTietHoaDonDto?> LayChiTietHoaDonAsync(int hoaDonId) => Task.FromResult<ChiTietHoaDonDto?>(null);
    }

    private class FakeThanhToanRepoMock : IThanhToanRepository
    {
        public Task<IEnumerable<ThanhToanListItemDto>> LayDanhSachThanhToanAsync() => Task.FromResult(Enumerable.Empty<ThanhToanListItemDto>());
        public Task<IEnumerable<HoaDonNoModel>> LayDanhSachHoaDonConNoTheoKhachHangAsync(int khachHangId) => Task.FromResult(Enumerable.Empty<HoaDonNoModel>());
        public Task<TaoThanhToanResult> GhiNhanThanhToanAsync(TaoThanhToanRequest request) => Task.FromResult(new TaoThanhToanResult { ThanhCong = true });
    }

    private class FakeBaoCaoTuoiNoRepoMock : IBaoCaoTuoiNoRepository
    {
        public Task<DuLieuBaoCaoTuoiNoDto> LayDuLieuBaoCaoAsync(DateTime ngayBaoCao) =>
            Task.FromResult(new DuLieuBaoCaoTuoiNoDto
            {
                RawData = new List<KhachHangHoaDonTuoiNoRawDto>(),
                TienThuChuaPhanBo = 0
            });

        public Task<IEnumerable<KhachHangHoaDonTuoiNoRawDto>> LayDuLieuTuoiNoRawAsync(DateTime ngayBaoCao) =>
            Task.FromResult(Enumerable.Empty<KhachHangHoaDonTuoiNoRawDto>());

        public Task<decimal> LayTongTienThuChuaPhanBoAsync(DateTime ngayBaoCao) => Task.FromResult(0m);
    }
}
