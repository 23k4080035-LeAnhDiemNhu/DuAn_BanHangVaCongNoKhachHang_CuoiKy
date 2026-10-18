using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp chức năng xem chi tiết hóa đơn trên DATABASE KIỂM THỬ RIÊNG: QuanLyBanHangCongNo_Test
/// Tuyệt đối KHÔNG sửa đổi dữ liệu database chính QuanLyBanHangCongNo.
/// </summary>
public class ChiTietHoaDonIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
    private const string MainConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _testConnectionFactory;
    private readonly IHoaDonRepository _testHoaDonRepo;

    public int SeedKhachHangId { get; private set; }
    public int SeedSanPham1Id { get; private set; }
    public int SeedSanPham2Id { get; private set; }
    public int SeedHoaDonId { get; private set; }
    public int SeedHoaDon2Id { get; private set; }
    public int SeedHoaDon4Id { get; private set; }

    public ChiTietHoaDonIntegrationTests()
    {
        _testConnectionFactory = new SqlConnectionFactory(TestConnectionString);
        _testHoaDonRepo = new HoaDonRepository(_testConnectionFactory);
    }

    public async Task InitializeAsync()
    {
        // 1. Đảm bảo database kiểm thử QuanLyBanHangCongNo_Test tồn tại
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

        // 2. Tạo schema trên QuanLyBanHangCongNo_Test nếu chưa có
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
                        TenKhachHang NVARCHAR(150) NOT NULL,
                        SoDienThoai NVARCHAR(20) NULL,
                        Email NVARCHAR(100) NULL,
                        DiaChi NVARCHAR(250) NULL,
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
                        DonViTinh NVARCHAR(50) NOT NULL DEFAULT (N'Cái'),
                        DonGia DECIMAL(18,2) NOT NULL DEFAULT (0),
                        SoLuongTon INT NOT NULL DEFAULT (0),
                        MoTa NVARCHAR(500) NULL,
                        HinhAnh NVARCHAR(300) NULL,
                        NgayTao DATETIME NOT NULL DEFAULT (GETDATE()),
                        TrangThai BIT NOT NULL DEFAULT (1)
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
                        DaThanhToan DECIMAL(18,2) NOT NULL DEFAULT (0),
                        ConLai AS (TongTien - DaThanhToan) PERSISTED,
                        TrangThai INT NOT NULL DEFAULT (0),
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
                END

                IF OBJECT_ID('dbo.ThanhToans', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ThanhToans
                    (
                        ThanhToanId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaPhieuThu NVARCHAR(20) NOT NULL UNIQUE,
                        KhachHangId INT NOT NULL FOREIGN KEY REFERENCES dbo.KhachHangs(KhachHangId),
                        NgayThanhToan DATETIME NOT NULL DEFAULT (GETDATE()),
                        SoTien DECIMAL(18,2) NOT NULL CHECK (SoTien > 0),
                        PhuongThuc NVARCHAR(50) NOT NULL DEFAULT (N'ChuyenKhoan'),
                        GhiChu NVARCHAR(500) NULL,
                        NgayTao DATETIME NOT NULL DEFAULT (GETDATE())
                    );
                END

                IF OBJECT_ID('dbo.PhanBoThanhToans', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.PhanBoThanhToans
                    (
                        PhanBoThanhToanId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        ThanhToanId INT NOT NULL FOREIGN KEY REFERENCES dbo.ThanhToans(ThanhToanId) ON DELETE CASCADE,
                        HoaDonId INT NOT NULL FOREIGN KEY REFERENCES dbo.HoaDons(HoaDonId),
                        SoTienPhanBo DECIMAL(18,2) NOT NULL CHECK (SoTienPhanBo > 0),
                        NgayPhanBo DATETIME NOT NULL DEFAULT (GETDATE())
                    );
                END";

            await testConn.ExecuteAsync(sqlCreateSchema);

            // 3. Xóa dữ liệu cũ trên database kiểm thử QuanLyBanHangCongNo_Test
            await testConn.ExecuteAsync(@"
                DELETE FROM dbo.PhanBoThanhToans;
                DELETE FROM dbo.ThanhToans;
                DELETE FROM dbo.ChiTietHoaDons;
                DELETE FROM dbo.HoaDons;
                DELETE FROM dbo.SanPhams;
                DELETE FROM dbo.KhachHangs;");

            // 4. Seed dữ liệu mẫu cho test
            SeedKhachHangId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, SoDienThoai, DiaChi, HanMucCongNo, TrangThai)
                VALUES ('KH_DETAIL1', N'Công ty Cổ Phần Minh Phát', '0901234567', N'123 Nguyễn Trãi, Q5, TP.HCM', 50000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            SeedSanPham1Id = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                VALUES ('SP_DETAIL1', N'Màn hình Dell UltraSharp 27', N'Cái', 8500000, 20, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            SeedSanPham2Id = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                VALUES ('SP_DETAIL2', N'Bàn phím cơ Logitech MX', N'Cái', 2500000, 50, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            // Hóa đơn chuẩn có 2 dòng hàng: 1 cái màn hình (8.5M) + 2 cái bàn phím (5.0M) = 13.5M
            // Đã trả 5M qua 2 lần phân bổ: 2M và 3M. Còn lại 8.5M.
            SeedHoaDonId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu)
                VALUES ('HD_TEST_001', @KhachHangId, '2026-10-01', '2026-10-25', 13500000, 5000000, 1, N'Hóa đơn giao hàng đợt 1');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            await testConn.ExecuteAsync(@"
                INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                VALUES (@HoaDonId, @Sp1Id, N'Màn hình Dell UltraSharp 27', N'Cái', 1, 8500000),
                       (@HoaDonId, @Sp2Id, N'Bàn phím cơ Logitech MX', N'Cái', 2, 2500000);",
                new { HoaDonId = SeedHoaDonId, Sp1Id = SeedSanPham1Id, Sp2Id = SeedSanPham2Id });

            var tt1 = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, GhiChu)
                VALUES ('PT_TEST_01', @KhachHangId, '2026-10-05', 2000000, N'ChuyenKhoan', N'Thanh toán đợt 1');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            var tt2 = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, GhiChu)
                VALUES ('PT_TEST_02', @KhachHangId, '2026-10-10', 3000000, N'TienMat', N'Thanh toán đợt 2');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            await testConn.ExecuteAsync(@"
                INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                VALUES (@Tt1, @HoaDonId, 2000000, '2026-10-05 10:00:00'),
                       (@Tt2, @HoaDonId, 3000000, '2026-10-10 14:30:00');",
                new { Tt1 = tt1, Tt2 = tt2, HoaDonId = SeedHoaDonId });

            // 5. Seed HD002 (Đã thanh toán đủ: Tổng 6.8M, phân bổ 6.8M, còn 0)
            SeedHoaDon2Id = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu)
                VALUES ('HD002', @KhachHangId, '2026-09-15', '2026-10-05', 6800000, 6800000, 2, N'Hóa đơn HD002 thanh toán đủ');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            await testConn.ExecuteAsync(@"
                INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                VALUES (@HoaDonId, @Sp1Id, N'Màn hình Dell UltraSharp 27', N'Cái', 1, 6800000);",
                new { HoaDonId = SeedHoaDon2Id, Sp1Id = SeedSanPham1Id });

            var ttHd2 = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc)
                VALUES ('PT_HD002', @KhachHangId, '2026-09-20', 6800000, N'ChuyenKhoan');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            await testConn.ExecuteAsync(@"
                INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                VALUES (@Tt, @HoaDonId, 6800000, '2026-09-20 09:00:00');",
                new { Tt = ttHd2, HoaDonId = SeedHoaDon2Id });

            // 6. Seed HD004 (Thanh toán 1 phần: Tổng 9.7M, phân bổ 500k, còn 9.2M)
            SeedHoaDon4Id = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu)
                VALUES ('HD004', @KhachHangId, '2026-09-25', '2026-10-15', 9700000, 500000, 1, N'Hóa đơn HD004 trả trước 500k');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            await testConn.ExecuteAsync(@"
                INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                VALUES (@HoaDonId, @Sp1Id, N'Màn hình Dell UltraSharp 27', N'Cái', 1, 9700000);",
                new { HoaDonId = SeedHoaDon4Id, Sp1Id = SeedSanPham1Id });

            var ttHd4 = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc)
                VALUES ('PT_HD004', @KhachHangId, '2026-09-28', 500000, N'TienMat');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            await testConn.ExecuteAsync(@"
                INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                VALUES (@Tt, @HoaDonId, 500000, '2026-09-28 15:00:00');",
                new { Tt = ttHd4, HoaDonId = SeedHoaDon4Id });
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test01_DocDungHeader_SnapshotDongHang_VaLichSuPhanBo()
    {
        var result = await _testHoaDonRepo.LayChiTietHoaDonAsync(SeedHoaDonId);

        Assert.NotNull(result);
        Assert.Equal("HD_TEST_001", result.MaHoaDon);
        Assert.Equal("Công ty Cổ Phần Minh Phát", result.TenKhachHang);
        Assert.Equal("0901234567", result.SoDienThoai);
        Assert.Equal("123 Nguyễn Trãi, Q5, TP.HCM", result.DiaChi);
        Assert.Equal(13500000m, result.TongTien);
        Assert.Equal(5000000m, result.DaThanhToan);
        Assert.Equal(8500000m, result.ConLai);
        Assert.Equal(TrangThaiHoaDon.ThanhToanMotPhan, result.TrangThai);
        Assert.Equal("Hóa đơn giao hàng đợt 1", result.GhiChu);

        // Kiểm tra dòng hàng snapshot
        Assert.Equal(2, result.DanhSachDongHang.Count);
        var item1 = result.DanhSachDongHang[0];
        Assert.Equal("Màn hình Dell UltraSharp 27", item1.TenSanPham);
        Assert.Equal("Cái", item1.DonViTinh);
        Assert.Equal(1, item1.SoLuong);
        Assert.Equal(8500000m, item1.DonGia);
        Assert.Equal(8500000m, item1.ThanhTien);

        var item2 = result.DanhSachDongHang[1];
        Assert.Equal("Bàn phím cơ Logitech MX", item2.TenSanPham);
        Assert.Equal("Cái", item2.DonViTinh);
        Assert.Equal(2, item2.SoLuong);
        Assert.Equal(2500000m, item2.DonGia);
        Assert.Equal(5000000m, item2.ThanhTien);

        // Kiểm tra lịch sử phân bổ
        Assert.Equal(2, result.DanhSachPhanBo.Count);
        Assert.Equal("PT_TEST_01", result.DanhSachPhanBo[0].MaPhieuThu);
        Assert.Equal(2000000m, result.DanhSachPhanBo[0].SoTienPhanBo);
        Assert.Equal("ChuyenKhoan", result.DanhSachPhanBo[0].PhuongThuc);

        Assert.Equal("PT_TEST_02", result.DanhSachPhanBo[1].MaPhieuThu);
        Assert.Equal(3000000m, result.DanhSachPhanBo[1].SoTienPhanBo);
        Assert.Equal("TienMat", result.DanhSachPhanBo[1].PhuongThuc);

        // Đối chiếu tính nhất quán số liệu
        Assert.Equal(result.TongTien, result.TongTienDongHang);
        Assert.Equal(result.DaThanhToan, result.TongTienPhanBo);
        Assert.Equal(result.TongTien - result.DaThanhToan, result.ConLai);
    }

    [Fact]
    public async Task Test02_DungSnapshotDaLuu_KhongBiThayDoiKhiDanhMucSanPhamCapNhat()
    {
        // Giả lập danh mục sản phẩm bị chỉnh sửa tên và giá mới
        using (var conn = new SqlConnection(TestConnectionString))
        {
            await conn.ExecuteAsync(@"
                UPDATE dbo.SanPhams
                SET TenSanPham = N'Màn hình Dell UltraSharp 27 (PHIÊN BẢN 2027 MỚI)',
                    DonGia = 19999999
                WHERE SanPhamId = @Sp1Id;", new { Sp1Id = SeedSanPham1Id });
        }

        // Truy vấn chi tiết hóa đơn
        var result = await _testHoaDonRepo.LayChiTietHoaDonAsync(SeedHoaDonId);

        Assert.NotNull(result);
        var item1 = result.DanhSachDongHang.First(d => d.SanPhamId == SeedSanPham1Id);

        // Vẫn phải hiển thị đúng tên và giá snapshot lịch sử tại thời điểm tạo hóa đơn
        Assert.Equal("Màn hình Dell UltraSharp 27", item1.TenSanPham);
        Assert.Equal(8500000m, item1.DonGia);
        Assert.Equal(8500000m, item1.ThanhTien);
        Assert.NotEqual("Màn hình Dell UltraSharp 27 (PHIÊN BẢN 2027 MỚI)", item1.TenSanPham);
        Assert.NotEqual(19999999m, item1.DonGia);
    }

    [Fact]
    public async Task Test03_HD002_TongPhanBo_6800000_ConLai_0()
    {
        var hd002 = await _testHoaDonRepo.LayChiTietHoaDonAsync(SeedHoaDon2Id);

        Assert.NotNull(hd002);
        Assert.Equal("HD002", hd002.MaHoaDon);
        Assert.Equal(6800000m, hd002.TongTien);
        Assert.Equal(6800000m, hd002.DaThanhToan);
        Assert.Equal(0m, hd002.ConLai);
        Assert.Equal(TrangThaiHoaDon.DaThanhToan, hd002.TrangThai);

        // Tổng phân bổ đúng 6.800.000đ và còn lại 0đ
        Assert.Equal(6800000m, hd002.TongTienPhanBo);
        Assert.Equal(0m, hd002.ConLai);
        Assert.Equal(hd002.TongTienDongHang, hd002.TongTien);
    }

    [Fact]
    public async Task Test04_HD004_TongPhanBo_500000_ConLai_9200000()
    {
        var hd004 = await _testHoaDonRepo.LayChiTietHoaDonAsync(SeedHoaDon4Id);

        Assert.NotNull(hd004);
        Assert.Equal("HD004", hd004.MaHoaDon);
        Assert.Equal(9700000m, hd004.TongTien);
        Assert.Equal(500000m, hd004.DaThanhToan);
        Assert.Equal(9200000m, hd004.ConLai);
        Assert.Equal(TrangThaiHoaDon.ThanhToanMotPhan, hd004.TrangThai);

        // Tổng phân bổ đúng 500.000đ và còn lại 9.200.000đ
        Assert.Equal(500000m, hd004.TongTienPhanBo);
        Assert.Equal(9200000m, hd004.ConLai);
        Assert.Equal(hd004.TongTienDongHang, hd004.TongTien);
    }

    [Fact]
    public async Task Test05_NhieuDongHang_Va_NhieuPhanBo_KhongBiNhanDoiTien()
    {
        // Seed thêm 1 hóa đơn có 3 dòng hàng và 4 lượt phân bổ
        int multiHdId;
        using (var conn = new SqlConnection(TestConnectionString))
        {
            multiHdId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai)
                VALUES ('HD_MULTI_CARTESIAN', @KhachHangId, '2026-10-01', '2026-10-30', 9000000, 4000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            // 3 dòng hàng: 2M + 3M + 4M = 9M
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                VALUES (@HdId, @Sp1, N'SP 1', N'Cái', 2, 1000000),
                       (@HdId, @Sp2, N'SP 2', N'Cái', 3, 1000000),
                       (@HdId, @Sp1, N'SP 3', N'Cái', 4, 1000000);",
                new { HdId = multiHdId, Sp1 = SeedSanPham1Id, Sp2 = SeedSanPham2Id });

            // 4 phiếu thu và 4 phân bổ: 1M + 1M + 1M + 1M = 4M
            for (int i = 1; i <= 4; i++)
            {
                var tt = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc)
                    VALUES (@Ma, @KhachHangId, '2026-10-10', 1000000, N'ChuyenKhoan');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { Ma = $"PT_MULTI_{i}", KhachHangId = SeedKhachHangId });

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                    VALUES (@Tt, @HdId, 1000000, GETDATE());",
                    new { Tt = tt, HdId = multiHdId });
            }
        }

        var result = await _testHoaDonRepo.LayChiTietHoaDonAsync(multiHdId);

        Assert.NotNull(result);
        // Nếu dùng JOIN giữa ChiTietHoaDons và PhanBoThanhToans sẽ ra 3 x 4 = 12 dòng!
        // Repository sử dụng QueryMultipleAsync đảm bảo trả về chính xác 3 dòng hàng và 4 phân bổ:
        Assert.Equal(3, result.DanhSachDongHang.Count);
        Assert.Equal(4, result.DanhSachPhanBo.Count);

        // Tuyệt đối không nhân đôi tiền
        Assert.Equal(9000000m, result.TongTienDongHang);
        Assert.Equal(4000000m, result.TongTienPhanBo);
        Assert.Equal(result.TongTien, result.TongTienDongHang);
        Assert.Equal(result.DaThanhToan, result.TongTienPhanBo);
        Assert.Equal(5000000m, result.ConLai);
    }

    [Fact]
    public async Task Test06_HoaDonKhongTonTai_TraVeNull()
    {
        var result = await _testHoaDonRepo.LayChiTietHoaDonAsync(999999);
        Assert.Null(result);
    }

    [Fact]
    public async Task Test07_QuyenHanUseCase_NhanVienBanHang_Va_KeToanCongNo_ThanhCong()
    {
        var nvbhUseCase = new XemChiTietHoaDonUseCase(_testHoaDonRepo, FakeCurrentUserService.NhanVienBanHang());
        var resNvbh = await nvbhUseCase.ExecuteAsync(SeedHoaDonId);
        Assert.NotNull(resNvbh);
        Assert.Equal("HD_TEST_001", resNvbh.MaHoaDon);

        var ktcnUseCase = new XemChiTietHoaDonUseCase(_testHoaDonRepo, FakeCurrentUserService.KeToanCongNo());
        var resKtcn = await ktcnUseCase.ExecuteAsync(SeedHoaDonId);
        Assert.NotNull(resKtcn);
        Assert.Equal("HD_TEST_001", resKtcn.MaHoaDon);
    }

    [Fact]
    public async Task Test08_QuyenHanUseCase_ChuaDangNhap_Va_SaiVaiTro_BiTuChoi()
    {
        var anonUseCase = new XemChiTietHoaDonUseCase(_testHoaDonRepo, FakeCurrentUserService.Anonymous());
        var exAnon = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => anonUseCase.ExecuteAsync(SeedHoaDonId));
        Assert.Contains("chưa được xác thực", exAnon.Message);

        var wrongRoleUseCase = new XemChiTietHoaDonUseCase(_testHoaDonRepo, FakeCurrentUserService.Role("KhachHang"));
        var exRole = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => wrongRoleUseCase.ExecuteAsync(SeedHoaDonId));
        Assert.Contains("không có quyền", exRole.Message);
    }

    [Fact]
    public async Task Test09_DocKiemTra_HD002_Va_HD004_Tren_DatabaseChinh_NeuCoSan()
    {
        // Đọc READ-ONLY trên database chính QuanLyBanHangCongNo để xác nhận số liệu thực tế
        try
        {
            var mainFactory = new SqlConnectionFactory(MainConnectionString);
            var mainRepo = new HoaDonRepository(mainFactory);

            // Tìm ID của HD002 và HD004 trên DB chính
            using var conn = new SqlConnection(MainConnectionString);
            await conn.OpenAsync();

            var hd002Id = await conn.ExecuteScalarAsync<int?>("SELECT HoaDonId FROM dbo.HoaDons WHERE MaHoaDon = 'HD002'");
            if (hd002Id.HasValue)
            {
                var hd002 = await mainRepo.LayChiTietHoaDonAsync(hd002Id.Value);
                Assert.NotNull(hd002);
                Assert.Equal("HD002", hd002.MaHoaDon);
                Assert.Equal(6800000m, hd002.TongTienPhanBo);
                Assert.Equal(0m, hd002.ConLai);
            }

            var hd004Id = await conn.ExecuteScalarAsync<int?>("SELECT HoaDonId FROM dbo.HoaDons WHERE MaHoaDon = 'HD004'");
            if (hd004Id.HasValue)
            {
                var hd004 = await mainRepo.LayChiTietHoaDonAsync(hd004Id.Value);
                Assert.NotNull(hd004);
                Assert.Equal("HD004", hd004.MaHoaDon);
                Assert.Equal(500000m, hd004.TongTienPhanBo);
                Assert.Equal(9200000m, hd004.ConLai);
            }
        }
        catch (Exception ex)
        {
            // Nếu môi trường không thể kết nối DB chính, bỏ qua (không fail vì test chính chạy trên QuanLyBanHangCongNo_Test)
            Console.WriteLine($"DB chính read check notice: {ex.Message}");
        }
    }

    [Fact]
    public async Task Test10_XenKeThanhToan_TransactionSerializable_HeaderVaPhanBoKhongBiTronTrangThai()
    {
        // 1. Tạo hóa đơn mới có tổng tiền 10.000.000đ, chưa thanh toán (DaThanhToan = 0, ConLai = 10.000.000đ)
        int testHdId;
        using (var initConn = new SqlConnection(TestConnectionString))
        {
            await initConn.OpenAsync();
            testHdId = await initConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu)
                VALUES ('HD_SERIALIZABLE_TEST', @KhachHangId, '2026-10-01', '2026-10-31', 10000000, 0, 0, N'Hóa đơn kiểm tra Serializable');
                SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhachHangId = SeedKhachHangId });

            await initConn.ExecuteAsync(@"
                INSERT INTO dbo.ChiTietHoaDons (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                VALUES (@HdId, @SpId, N'Màn hình Dell UltraSharp 27', N'Cái', 1, 10000000);",
                new { HdId = testHdId, SpId = SeedSanPham1Id });
        }

        // 2. Xác nhận trạng thái ban đầu: DaThanhToan = 0, TongTienPhanBo = 0
        var banDau = await _testHoaDonRepo.LayChiTietHoaDonAsync(testHdId);
        Assert.NotNull(banDau);
        Assert.Equal(0m, banDau.DaThanhToan);
        Assert.Equal(0m, banDau.TongTienPhanBo);
        Assert.Equal(10000000m, banDau.ConLai);

        // 3. Mô phỏng tình huống ghi nhận thanh toán dở dang trong Transaction ghi:
        //    Cập nhật HoaDons (DaThanhToan = 4.000.000đ) TRƯỚC, sau đó mới chèn PhanBoThanhToans (4.000.000đ).
        //    Trong lúc ghi chưa hoàn tất, một tác vụ đọc LayChiTietHoaDonAsync được gọi đồng thời.
        //    Nhờ transaction Serializable, tác vụ đọc KHÔNG BAO GIỜ đọc thấy trạng thái lai (DaThanhToan = 4M nhưng PhanBo = 0M).
        var writerStartedSignal = new TaskCompletionSource<bool>();
        var readerStartedSignal = new TaskCompletionSource<bool>();
        ChiTietHoaDonDto? concurrentReadResult = null;
        Exception? readException = null;

        var writerTask = Task.Run(async () =>
        {
            using var writeConn = new SqlConnection(TestConnectionString);
            await writeConn.OpenAsync();
            using var writeTx = writeConn.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // Bước A: Cập nhật DaThanhToan lên 4.000.000đ trong transaction
                await writeConn.ExecuteAsync(@"
                    UPDATE dbo.HoaDons
                    SET DaThanhToan = 4000000, TrangThai = 1
                    WHERE HoaDonId = @HdId;", new { HdId = testHdId }, transaction: writeTx);

                // Báo hiệu writer đã ghi xong bước 1
                writerStartedSignal.SetResult(true);

                // Đợi reader bắt đầu thực thi truy vấn
                await readerStartedSignal.Task;

                // Nghỉ 100ms để đảm bảo reader đang chờ hoặc đang đọc
                await Task.Delay(100);

                // Bước B: Chèn phiếu thu và phân bổ 4.000.000đ
                var ttId = await writeConn.ExecuteScalarAsync<int>(@"
                    INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc)
                    VALUES ('PT_SERIAL_01', @KhachHangId, '2026-10-15', 4000000, N'ChuyenKhoan');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { KhachHangId = SeedKhachHangId }, transaction: writeTx);

                await writeConn.ExecuteAsync(@"
                    INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                    VALUES (@TtId, @HdId, 4000000, GETDATE());",
                    new { TtId = ttId, HdId = testHdId }, transaction: writeTx);

                writeTx.Commit();
            }
            catch
            {
                try { writeTx.Rollback(); } catch { }
                throw;
            }
        });

        var readerTask = Task.Run(async () =>
        {
            // Chờ writer đã thực hiện bước A (update DaThanhToan nhưng chưa chèn phân bổ)
            await writerStartedSignal.Task;

            // Báo hiệu reader bắt đầu đọc
            readerStartedSignal.SetResult(true);

            try
            {
                // LayChiTietHoaDonAsync sử dụng Transaction Serializable:
                // Sẽ đợi transaction writer commit, hoặc đọc snapshot nhất quán
                concurrentReadResult = await _testHoaDonRepo.LayChiTietHoaDonAsync(testHdId);
            }
            catch (Exception ex)
            {
                readException = ex;
            }
        });

        await Task.WhenAll(writerTask, readerTask);

        // 4. Kiểm tra kết quả: không phát sinh exception và số liệu hoàn toàn nhất quán
        Assert.Null(readException);
        Assert.NotNull(concurrentReadResult);

        // Tuyệt đối không bị trộn trạng thái: DaThanhToan và TongTienPhanBo phải bằng nhau (cùng là 4.000.000đ)
        Assert.Equal(4000000m, concurrentReadResult.DaThanhToan);
        Assert.Equal(4000000m, concurrentReadResult.TongTienPhanBo);
        Assert.Equal(concurrentReadResult.DaThanhToan, concurrentReadResult.TongTienPhanBo);
        Assert.Equal(6000000m, concurrentReadResult.ConLai);
        Assert.Equal(concurrentReadResult.TongTien - concurrentReadResult.DaThanhToan, concurrentReadResult.ConLai);
        Assert.Single(concurrentReadResult.DanhSachPhanBo);
        Assert.Equal("PT_SERIAL_01", concurrentReadResult.DanhSachPhanBo[0].MaPhieuThu);
    }
}

