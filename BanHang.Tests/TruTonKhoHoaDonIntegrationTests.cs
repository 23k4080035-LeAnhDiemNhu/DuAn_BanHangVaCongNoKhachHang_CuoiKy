using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.SanPhamScreen;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp nghiệp vụ trừ tồn kho tự động khi tạo hóa đơn bán chịu thành công.
/// Đảm bảo tính nguyên tử, tuần tự hóa khóa chống bán vượt tồn đồng thời và rollback toàn bộ khi lỗi.
/// CHỈ CHẠY TRÊN DATABASE KIỂM THỬ: QuanLyBanHangCongNo_Test.
/// </summary>
public class TruTonKhoHoaDonIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IHoaDonRepository _hoaDonRepo;
    private readonly ITaoHoaDonBanChiuUseCase _taoHoaDonUseCase;

    public int KhachHangTestId { get; private set; }

    public TruTonKhoHoaDonIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        var nvbhUser = FakeCurrentUserService.NhanVienBanHang();
        _hoaDonRepo = new HoaDonRepository(_connectionFactory);
        _taoHoaDonUseCase = new TaoHoaDonBanChiuUseCase(_hoaDonRepo, nvbhUser);
    }

    public async Task InitializeAsync()
    {
        using (var masterConn = new SqlConnection(MasterConnectionString))
        {
            await masterConn.OpenAsync();
            const string sqlCreateDb = @"
                IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'QuanLyBanHangCongNo_Test')
                BEGIN
                    CREATE DATABASE QuanLyBanHangCongNo_Test;
                END";
            await masterConn.ExecuteAsync(sqlCreateDb);
        }

        using (var testConn = new SqlConnection(TestConnectionString))
        {
            await testConn.OpenAsync();
            const string sqlCreateSchema = @"
                IF OBJECT_ID('dbo.KhachHangs', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.KhachHangs
                    (
                        KhachHangId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaKhachHang NVARCHAR(20) NOT NULL UNIQUE,
                        TenKhachHang NVARCHAR(200) NOT NULL,
                        SoDienThoai NVARCHAR(20) NULL,
                        Email NVARCHAR(100) NULL,
                        DiaChi NVARCHAR(300) NULL,
                        HanMucCongNo DECIMAL(18,2) NOT NULL DEFAULT (0),
                        NgayTao DATETIME NOT NULL DEFAULT (GETDATE()),
                        TrangThai BIT NOT NULL DEFAULT (1)
                    );
                END

                IF OBJECT_ID('dbo.SanPhams', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.SanPhams
                    (
                        SanPhamId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaSanPham NVARCHAR(20) NOT NULL UNIQUE,
                        TenSanPham NVARCHAR(200) NOT NULL,
                        DonViTinh NVARCHAR(50) NOT NULL,
                        DonGia DECIMAL(18,2) NOT NULL DEFAULT (0),
                        SoLuongTon INT NOT NULL DEFAULT (0),
                        MoTa NVARCHAR(500) NULL,
                        HinhAnh NVARCHAR(300) NULL,
                        NgayTao DATETIME NOT NULL DEFAULT (GETDATE()),
                        TrangThai BIT NOT NULL DEFAULT (1),
                        CONSTRAINT CK_SanPhams_SoLuongTon CHECK (SoLuongTon >= 0)
                    );
                END

                IF OBJECT_ID('dbo.HoaDons', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.HoaDons
                    (
                        HoaDonId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaHoaDon NVARCHAR(20) NOT NULL UNIQUE,
                        KhachHangId INT NOT NULL FOREIGN KEY REFERENCES dbo.KhachHangs(KhachHangId),
                        NgayLap DATETIME NOT NULL DEFAULT (GETDATE()),
                        HanThanhToan DATETIME NOT NULL,
                        TongTien DECIMAL(18,2) NOT NULL DEFAULT (0),
                        SoTienDaThanhToan DECIMAL(18,2) NOT NULL DEFAULT (0),
                        TrangThai NVARCHAR(50) NOT NULL DEFAULT (N'ChuaThanhToan'),
                        GhiChu NVARCHAR(500) NULL,
                        NgayTao DATETIME NOT NULL DEFAULT (GETDATE())
                    );
                END

                IF OBJECT_ID('dbo.ChiTietHoaDons', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ChiTietHoaDons
                    (
                        ChiTietHoaDonId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        HoaDonId INT NOT NULL FOREIGN KEY REFERENCES dbo.HoaDons(HoaDonId) ON DELETE CASCADE,
                        SanPhamId INT NOT NULL FOREIGN KEY REFERENCES dbo.SanPhams(SanPhamId),
                        TenSanPham NVARCHAR(200) NOT NULL,
                        DonViTinh NVARCHAR(50) NOT NULL,
                        SoLuong INT NOT NULL CHECK (SoLuong > 0),
                        DonGia DECIMAL(18,2) NOT NULL CHECK (DonGia >= 0),
                        ThanhTien AS (SoLuong * DonGia) PERSISTED
                    );
                END";

            await testConn.ExecuteAsync(sqlCreateSchema);

            await testConn.ExecuteAsync(@"
                IF OBJECT_ID('dbo.PhanBoThanhToans', 'U') IS NOT NULL DELETE FROM dbo.PhanBoThanhToans;
                IF OBJECT_ID('dbo.ThanhToans', 'U') IS NOT NULL DELETE FROM dbo.ThanhToans;
                DELETE FROM dbo.ChiTietHoaDons;
                DELETE FROM dbo.HoaDons;
                DELETE FROM dbo.SanPhams;
                DELETE FROM dbo.KhachHangs;");

            KhachHangTestId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_TON_HD', N'Khách Hàng Test Trừ Tồn', 500000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test01_BanMotPhan_TruDungSoLuongBan_VaLuuHoaDonThanhCong()
    {
        using var conn = new SqlConnection(TestConnectionString);
        int spId = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_TRU_01', N'Màn Hình Dell 27 inch', N'Cái', 4500000, 20, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_TRU_01",
            KhachHangId = KhachHangTestId,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new() { SanPhamId = spId, SoLuong = 7, DonGiaHienThi = 4500000 }
            }
        };

        var result = await _taoHoaDonUseCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong, result.ThongBao);

        // Kiểm tra tồn kho trong database: 20 - 7 = 13
        int tonSauBan = await conn.ExecuteScalarAsync<int>(
            "SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        Assert.Equal(13, tonSauBan);

        // Kiểm tra hóa đơn và chi tiết được lưu đúng
        var hd = await conn.QuerySingleOrDefaultAsync<HoaDon>(
            "SELECT * FROM dbo.HoaDons WHERE HoaDonId = @Id", new { Id = result.HoaDonIdMoi });
        Assert.NotNull(hd);
        Assert.Equal(7 * 4500000, hd.TongTien);
    }

    [Fact]
    public async Task Test02_BanVuaHet_TonVe0_VaLuuHoaDonThanhCong()
    {
        using var conn = new SqlConnection(TestConnectionString);
        int spId = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_TRU_02', N'Bàn Phím Cơ Custom', N'Chiếc', 2000000, 15, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_TRU_02",
            KhachHangId = KhachHangTestId,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new() { SanPhamId = spId, SoLuong = 15, DonGiaHienThi = 2000000 } // Bán hết sạch 15 cái
            }
        };

        var result = await _taoHoaDonUseCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong, result.ThongBao);

        // Tồn kho phải về đúng 0
        int tonSauBan = await conn.ExecuteScalarAsync<int>(
            "SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        Assert.Equal(0, tonSauBan);
    }

    [Fact]
    public async Task Test03_ThieuTon_TuChoiToanBoHoaDon_BaoLoiTenSpTonHienTaiVaSoLuongYeuCau()
    {
        using var conn = new SqlConnection(TestConnectionString);
        int spId = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_TRU_03', N'Card Đồ Họa RTX 4090', N'Cái', 45000000, 5, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_TRU_03",
            KhachHangId = KhachHangTestId,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new() { SanPhamId = spId, SoLuong = 8, DonGiaHienThi = 45000000 } // Tồn 5, yêu cầu 8 -> Thiếu
            }
        };

        var result = await _taoHoaDonUseCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("Card Đồ Họa RTX 4090", result.ThongBao);
        Assert.Contains("5", result.ThongBao); // Tồn hiện tại
        Assert.Contains("8", result.ThongBao); // Yêu cầu

        // Kiểm tra tồn kho không đổi
        int tonHienTai = await conn.ExecuteScalarAsync<int>(
            "SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        Assert.Equal(5, tonHienTai);

        // Kiểm tra không có hóa đơn nào được tạo
        int countHd = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.HoaDons WHERE MaHoaDon = 'HD_TRU_03'");
        Assert.Equal(0, countHd);
    }

    [Fact]
    public async Task Test04_NhieuSanPham_MotSanPhamThieuTon_TuChoiToanBoHoaDon_KhongSanPhamNaoBiTru()
    {
        using var conn = new SqlConnection(TestConnectionString);
        int sp1Id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_MULTI_1', N'Chuột Không Dây', N'Cái', 500000, 50, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        int sp2Id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_MULTI_2', N'Lót Chuột RGB', N'Cái', 300000, 10, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        int sp3Id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_MULTI_3', N'Loa Vi Tính Hi-Fi', N'Bộ', 1500000, 3, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_MULTI_FAIL",
            KhachHangId = KhachHangTestId,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new() { SanPhamId = sp1Id, SoLuong = 10, DonGiaHienThi = 500000 },  // Đủ (50 >= 10)
                new() { SanPhamId = sp2Id, SoLuong = 5, DonGiaHienThi = 300000 },   // Đủ (10 >= 5)
                new() { SanPhamId = sp3Id, SoLuong = 5, DonGiaHienThi = 1500000 }   // THIẾU (3 < 5)
            }
        };

        var result = await _taoHoaDonUseCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("Loa Vi Tính Hi-Fi", result.ThongBao);
        Assert.Contains("3", result.ThongBao);
        Assert.Contains("5", result.ThongBao);

        // Kiểm tra TUYỆT ĐỐI không có sản phẩm nào bị trừ tồn (SP1 vẫn 50, SP2 vẫn 10, SP3 vẫn 3)
        int ton1 = await conn.ExecuteScalarAsync<int>("SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = sp1Id });
        int ton2 = await conn.ExecuteScalarAsync<int>("SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = sp2Id });
        int ton3 = await conn.ExecuteScalarAsync<int>("SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = sp3Id });

        Assert.Equal(50, ton1);
        Assert.Equal(10, ton2);
        Assert.Equal(3, ton3);

        // Không có hóa đơn hay chi tiết nào được ghi vào hệ thống
        int countHd = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.HoaDons WHERE MaHoaDon = 'HD_MULTI_FAIL'");
        Assert.Equal(0, countHd);
    }

    [Fact]
    public async Task Test05_BanDongThoi_KhoaSanPhamBaoVe_KhongVuotTon_TonKhongBiAm()
    {
        using var conn = new SqlConnection(TestConnectionString);
        // Sản phẩm tồn kho = 10 cái
        int spId = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_CONCURRENCY', N'Điện Thoại Giới Hạn', N'Chiếc', 10000000, 10, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        // 2 giao dịch đồng thời, mỗi giao dịch muốn mua 6 cái (tổng yêu cầu = 12 > 10)
        var req1 = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_CONCUR_A",
            KhachHangId = KhachHangTestId,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new() { SanPhamId = spId, SoLuong = 6, DonGiaHienThi = 10000000 }
            }
        };

        var req2 = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_CONCUR_B",
            KhachHangId = KhachHangTestId,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new() { SanPhamId = spId, SoLuong = 6, DonGiaHienThi = 10000000 }
            }
        };

        // Kích hoạt song song
        var task1 = _taoHoaDonUseCase.ExecuteAsync(req1);
        var task2 = _taoHoaDonUseCase.ExecuteAsync(req2);

        var results = await Task.WhenAll(task1, task2);

        // Chỉ có ĐÚNG 1 giao dịch được phép thành công, 1 giao dịch phải bị từ chối
        int soThanhCong = results.Count(r => r.ThanhCong);
        int soThatBai = results.Count(r => !r.ThanhCong);

        Assert.Equal(1, soThanhCong);
        Assert.Equal(1, soThatBai);

        // Kiểm tra tồn kho còn lại: 10 - 6 = 4 (tuyệt đối >= 0, không bao giờ âm)
        int tonCuoi = await conn.ExecuteScalarAsync<int>(
            "SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        Assert.Equal(4, tonCuoi);
    }
}
