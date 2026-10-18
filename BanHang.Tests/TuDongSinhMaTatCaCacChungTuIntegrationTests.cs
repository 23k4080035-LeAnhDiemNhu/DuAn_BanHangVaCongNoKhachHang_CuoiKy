using System.Data;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.SanPhamScreen;
using BanHang.UseCases.ThanhToanScreen;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp tự động cấp mã cho tất cả các đối tượng: Khách hàng (KHxxx), Sản phẩm (SPxxx), Phiếu thu (PTxxx)
/// CHỈ chạy trên database kiểm thử riêng: QuanLyBanHangCongNo_Test
/// </summary>
public class TuDongSinhMaTatCaCacChungTuIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IKhachHangRepository _khachHangRepo;
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly IThanhToanRepository _thanhToanRepo;
    private readonly IThemKhachHangUseCase _themKhachHangUseCase;
    private readonly IThemSanPhamUseCase _themSanPhamUseCase;
    private readonly ITaoThanhToanUseCase _taoThanhToanUseCase;

    public TuDongSinhMaTatCaCacChungTuIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        _khachHangRepo = new KhachHangRepository(_connectionFactory);
        _sanPhamRepo = new SanPhamRepository(_connectionFactory);
        _thanhToanRepo = new ThanhToanRepository(_connectionFactory);

        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        _themKhachHangUseCase = new ThemKhachHangUseCase(_khachHangRepo, ktcnUser);
        _themSanPhamUseCase = new ThemSanPhamUseCase(_sanPhamRepo, ktcnUser);
        _taoThanhToanUseCase = new TaoThanhToanUseCase(_thanhToanRepo, ktcnUser);
    }

    public async Task InitializeAsync()
    {
        using (var masterConn = new SqlConnection(MasterConnectionString))
        {
            await masterConn.OpenAsync();
            const string sqlCreateDb = @"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'QuanLyBanHangCongNo_Test')
                BEGIN
                    CREATE DATABASE [QuanLyBanHangCongNo_Test];
                END";
            await masterConn.ExecuteAsync(sqlCreateDb);
        }

        using (var testConn = new SqlConnection(TestConnectionString))
        {
            await testConn.OpenAsync();

            await testConn.ExecuteAsync(@"
                DELETE FROM dbo.PhanBoThanhToans;
                DELETE FROM dbo.ThanhToans;
                DELETE FROM dbo.ChiTietHoaDons;
                DELETE FROM dbo.HoaDons;
                DELETE FROM dbo.SanPhams;
                DELETE FROM dbo.KhachHangs;");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test01_KhachHang_TuDongSinhMa_TuanTu_KH001_KH002()
    {
        // 1. Khi bảng rỗng, mã tiếp theo là KH001
        var nextCode = await _khachHangRepo.LayMaKhachHangTiepTheoAsync();
        Assert.Equal("KH001", nextCode);

        // 2. Thêm khách hàng không truyền mã -> tự động sinh KH001
        var result1 = await _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            TenKhachHang = "Công ty TNHH Khách Hàng 1",
            HanMucCongNo = 10000000
        });
        Assert.True(result1.ThanhCong);
        Assert.Equal("KH001", result1.MaKhachHangMoi);

        // 3. Mã tiếp theo dự kiến là KH002
        var nextCode2 = await _khachHangRepo.LayMaKhachHangTiepTheoAsync();
        Assert.Equal("KH002", nextCode2);

        // 4. Thêm khách hàng tiếp theo -> sinh KH002
        var result2 = await _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            TenKhachHang = "Công ty Cổ Phần Khách Hàng 2",
            HanMucCongNo = 20000000
        });
        Assert.True(result2.ThanhCong);
        Assert.Equal("KH002", result2.MaKhachHangMoi);
    }

    [Fact]
    public async Task Test02_SanPham_TuDongSinhMa_TuanTu_SP001_SP002()
    {
        // 1. Khi bảng rỗng, mã tiếp theo là SP001
        var nextCode = await _sanPhamRepo.LayMaSanPhamTiepTheoAsync();
        Assert.Equal("SP001", nextCode);

        // 2. Thêm sản phẩm không truyền mã -> tự động sinh SP001
        var result1 = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            TenSanPham = "Bàn phím cơ không dây",
            DonViTinh = "Cái",
            DonGia = 1500000
        });
        Assert.True(result1.ThanhCong);
        Assert.Equal("SP001", result1.MaSanPhamMoi);

        // 3. Mã tiếp theo dự kiến là SP002
        var nextCode2 = await _sanPhamRepo.LayMaSanPhamTiepTheoAsync();
        Assert.Equal("SP002", nextCode2);

        // 4. Thêm sản phẩm tiếp theo -> sinh SP002
        var result2 = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            TenSanPham = "Chuột gaming công thái học",
            DonViTinh = "Cái",
            DonGia = 890000
        });
        Assert.True(result2.ThanhCong);
        Assert.Equal("SP002", result2.MaSanPhamMoi);
    }

    [Fact]
    public async Task Test03_ThanhToan_TuDongSinhMa_TuanTu_PT001_PT002()
    {
        // Tạo trước một khách hàng để thanh toán
        int khachHangId;
        using (var conn = new SqlConnection(TestConnectionString))
        {
            await conn.OpenAsync();
            khachHangId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_TEST_PT', N'Khách Hàng Nộp Tiền', 50000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }

        // 1. Khi bảng rỗng, mã tiếp theo là PT001
        var nextCode = await _thanhToanRepo.LayMaPhieuThuTiepTheoAsync();
        Assert.Equal("PT001", nextCode);

        // 2. Tạo thanh toán không truyền mã -> tự động sinh PT001
        var result1 = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            KhachHangId = khachHangId,
            NgayThanhToan = DateTime.Today,
            SoTien = 1000000,
            PhuongThuc = "ChuyenKhoan"
        });
        Assert.True(result1.ThanhCong);
        Assert.Equal("PT001", result1.MaPhieuThu);

        // 3. Mã tiếp theo là PT002
        var nextCode2 = await _thanhToanRepo.LayMaPhieuThuTiepTheoAsync();
        Assert.Equal("PT002", nextCode2);

        // 4. Tạo thanh toán thứ 2 -> tự động sinh PT002
        var result2 = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            KhachHangId = khachHangId,
            NgayThanhToan = DateTime.Today,
            SoTien = 2000000,
            PhuongThuc = "TienMat"
        });
        Assert.True(result2.ThanhCong);
        Assert.Equal("PT002", result2.MaPhieuThu);
    }

    [Fact]
    public async Task Test04_KhachHang_DongThoi_KhongTrungMa()
    {
        // Chạy đồng thời 5 request tạo khách hàng không truyền mã
        var tasks = Enumerable.Range(1, 5).Select(i => _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            TenKhachHang = $"Khách Hàng Concurrent {i}",
            HanMucCongNo = 10000000
        })).ToList();

        var results = await Task.WhenAll(tasks);

        // Tất cả phải thành công
        Assert.All(results, r => Assert.True(r.ThanhCong));

        // Tất cả mã mới sinh phải là duy nhất
        var maMoi = results.Select(r => r.MaKhachHangMoi).ToList();
        Assert.Equal(5, maMoi.Distinct().Count());
    }

    [Fact]
    public async Task Test05_SanPham_DongThoi_KhongTrungMa()
    {
        // Chạy đồng thời 5 request tạo sản phẩm không truyền mã
        var tasks = Enumerable.Range(1, 5).Select(i => _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            TenSanPham = $"Sản Phẩm Concurrent {i}",
            DonViTinh = "Cái",
            DonGia = 100000 * i
        })).ToList();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.ThanhCong));
        var maMoi = results.Select(r => r.MaSanPhamMoi).ToList();
        Assert.Equal(5, maMoi.Distinct().Count());
    }

    [Fact]
    public async Task Test06_ThanhToan_DongThoi_KhongTrungMa()
    {
        int khachHangId;
        using (var conn = new SqlConnection(TestConnectionString))
        {
            await conn.OpenAsync();
            khachHangId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_TEST_PT_CONC', N'Khách Hàng Nộp Tiền Concurrent', 50000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }

        // Chạy đồng thời 5 request ghi nhận thanh toán không truyền mã
        var tasks = Enumerable.Range(1, 5).Select(i => _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            KhachHangId = khachHangId,
            NgayThanhToan = DateTime.Today,
            SoTien = 100000 * i,
            PhuongThuc = "ChuyenKhoan"
        })).ToList();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.ThanhCong));
        var maMoi = results.Select(r => r.MaPhieuThu).ToList();
        Assert.Equal(5, maMoi.Distinct().Count());
    }
}
