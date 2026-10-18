using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.SanPhamScreen;
using BanHang.UseCases.ThanhToanScreen;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp xử lý kết quả và timeout của sp_getapplock:
/// - Khi application lock bị chiếm giữ bởi connection khác -> ép timeout
/// - Nhận mã kết quả âm (-1) đúng cách, dừng thao tác và rollback toàn bộ transaction
/// - Xác nhận TUYỆT ĐỐI không để lại dữ liệu rác, không sinh mã và không thay đổi tồn kho/công nợ.
/// CHỈ CHẠY TRÊN DATABASE KIỂM THỬ: QuanLyBanHangCongNo_Test
/// </summary>
public class AppLockTimeoutIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IHoaDonRepository _hoaDonRepo;
    private readonly IKhachHangRepository _khachHangRepo;
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly IThanhToanRepository _thanhToanRepo;

    public AppLockTimeoutIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        _hoaDonRepo = new HoaDonRepository(_connectionFactory);
        _khachHangRepo = new KhachHangRepository(_connectionFactory);
        _sanPhamRepo = new SanPhamRepository(_connectionFactory);
        _thanhToanRepo = new ThanhToanRepository(_connectionFactory);
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
                        NgayTao DATETIME NOT NULL DEFAULT (GETDATE())
                    );
                END";

            await testConn.ExecuteAsync(sqlCreateSchema);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test01_TaoHoaDon_BiKiemGiuAppLock_EpTimeout_TuChoiVaKhongThayDoiTonKhoVaCongNo()
    {
        using var setupConn = new SqlConnection(TestConnectionString);
        await setupConn.OpenAsync();

        // 1. Chuẩn bị khách hàng và sản phẩm test
        string maKh = $"KH_LOCK_{Guid.NewGuid():N}"[..10];
        int khId = await setupConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
            VALUES (@maKh, N'Khách Hàng Test Lock', 50000000, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", new { maKh });

        string maSp = $"SP_LOCK_{Guid.NewGuid():N}"[..10];
        int spId = await setupConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES (@maSp, N'Sản Phẩm Test Lock', N'Cái', 1000000, 100, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", new { maSp });

        // 2. Mở Connection khác và chiếm giữ Exclusive AppLock 'QuanLyBanHang_CapMaHoaDon'
        using var holdingConn = new SqlConnection(TestConnectionString);
        await holdingConn.OpenAsync();
        using var holdingTrans = holdingConn.BeginTransaction();

        const string sqlHoldLock = @"
            DECLARE @r INT;
            EXEC @r = sp_getapplock 
                @Resource = 'QuanLyBanHang_CapMaHoaDon', 
                @LockMode = 'Exclusive', 
                @LockOwner = 'Transaction', 
                @LockTimeout = -1,
                @DbPrincipal = 'public';
            SELECT @r;";
        var holdResult = await holdingConn.ExecuteScalarAsync<int>(sqlHoldLock, transaction: holdingTrans);
        Assert.True(holdResult >= 0, "Connection giữ lock phải nhận được lock thành công.");

        try
        {
            // 3. Gọi tạo hóa đơn trên connection của repository -> sẽ bị chờ và timeout sau 5 giây
            var request = new TaoHoaDonRequest
            {
                KhachHangId = khId,
                NgayLap = DateTime.Today,
                HanThanhToan = DateTime.Today.AddDays(30),
                GhiChu = "Đơn hàng test ép timeout lock",
                ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
                {
                    new() { SanPhamId = spId, SoLuong = 10, DonGiaHienThi = 1000000 }
                }
            };

            var result = await _hoaDonRepo.TaoHoaDonBanChiuAsync(request);

            // 4. Xác nhận bị từ chối với thông báo thân thiện
            Assert.False(result.ThanhCong);
            Assert.Contains("quá thời gian chờ", result.ThongBao);

            // 5. Kiểm tra DB: Tồn kho KHÔNG ĐỔI (vẫn 100, không bị trừ 10)
            int tonSauDo = await setupConn.ExecuteScalarAsync<int>(
                "SELECT SoLuongTon FROM dbo.SanPhams WHERE SanPhamId = @spId;", new { spId });
            Assert.Equal(100, tonSauDo);

            // Kiểm tra DB: Không có hóa đơn nào được lưu cho khách hàng này
            int countHd = await setupConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.HoaDons WHERE KhachHangId = @khId;", new { khId });
            Assert.Equal(0, countHd);

            // Kiểm tra DB: Không có chi tiết hóa đơn nào
            int countCt = await setupConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.ChiTietHoaDons WHERE SanPhamId = @spId;", new { spId });
            Assert.Equal(0, countCt);
        }
        finally
        {
            holdingTrans.Rollback();
        }
    }

    [Fact]
    public async Task Test02_ThemKhachHang_BiKiemGiuAppLock_EpTimeout_TuChoiVaKhongChenDuLieu()
    {
        using var setupConn = new SqlConnection(TestConnectionString);
        await setupConn.OpenAsync();

        // Mở connection khác và chiếm giữ Exclusive AppLock 'QuanLyBanHang_CapMaKhachHang'
        using var holdingConn = new SqlConnection(TestConnectionString);
        await holdingConn.OpenAsync();
        using var holdingTrans = holdingConn.BeginTransaction();

        const string sqlHoldLock = @"
            DECLARE @r INT;
            EXEC @r = sp_getapplock 
                @Resource = 'QuanLyBanHang_CapMaKhachHang', 
                @LockMode = 'Exclusive', 
                @LockOwner = 'Transaction', 
                @LockTimeout = -1,
                @DbPrincipal = 'public';
            SELECT @r;";
        var holdResult = await holdingConn.ExecuteScalarAsync<int>(sqlHoldLock, transaction: holdingTrans);
        Assert.True(holdResult >= 0);

        string tenKhMoi = $"Khách Hàng Timeout {Guid.NewGuid():N}"[..25];

        try
        {
            var request = new TaoKhachHangRequest
            {
                TenKhachHang = tenKhMoi,
                HanMucCongNo = 10000000,
                TrangThai = true
            };

            var result = await _khachHangRepo.ThemKhachHangAsync(request);

            Assert.False(result.ThanhCong);
            Assert.Contains("quá thời gian chờ", result.ThongBao);

            // Xác nhận không có bản ghi khách hàng nào được chèn
            int countKh = await setupConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.KhachHangs WHERE TenKhachHang = @tenKhMoi;", new { tenKhMoi });
            Assert.Equal(0, countKh);
        }
        finally
        {
            holdingTrans.Rollback();
        }
    }

    [Fact]
    public async Task Test03_ThemSanPham_BiKiemGiuAppLock_EpTimeout_TuChoiVaKhongChenDuLieu()
    {
        using var setupConn = new SqlConnection(TestConnectionString);
        await setupConn.OpenAsync();

        // Mở connection khác và chiếm giữ Exclusive AppLock 'QuanLyBanHang_CapMaSanPham'
        using var holdingConn = new SqlConnection(TestConnectionString);
        await holdingConn.OpenAsync();
        using var holdingTrans = holdingConn.BeginTransaction();

        const string sqlHoldLock = @"
            DECLARE @r INT;
            EXEC @r = sp_getapplock 
                @Resource = 'QuanLyBanHang_CapMaSanPham', 
                @LockMode = 'Exclusive', 
                @LockOwner = 'Transaction', 
                @LockTimeout = -1,
                @DbPrincipal = 'public';
            SELECT @r;";
        var holdResult = await holdingConn.ExecuteScalarAsync<int>(sqlHoldLock, transaction: holdingTrans);
        Assert.True(holdResult >= 0);

        string tenSpMoi = $"Sản Phẩm Timeout {Guid.NewGuid():N}"[..25];

        try
        {
            var request = new TaoSanPhamRequest
            {
                TenSanPham = tenSpMoi,
                DonViTinh = "Cái",
                DonGia = 250000,
                SoLuongTon = 20,
                TrangThai = true
            };

            var result = await _sanPhamRepo.ThemSanPhamAsync(request);

            Assert.False(result.ThanhCong);
            Assert.Contains("quá thời gian chờ", result.ThongBao);

            // Xác nhận không có bản ghi sản phẩm nào được chèn
            int countSp = await setupConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.SanPhams WHERE TenSanPham = @tenSpMoi;", new { tenSpMoi });
            Assert.Equal(0, countSp);
        }
        finally
        {
            holdingTrans.Rollback();
        }
    }

    [Fact]
    public async Task Test04_TaoThanhToan_BiKiemGiuAppLock_EpTimeout_TuChoiVaKhongThayDoiCongNo()
    {
        using var setupConn = new SqlConnection(TestConnectionString);
        await setupConn.OpenAsync();

        // Chuẩn bị khách hàng và hóa đơn có nợ
        string maKh = $"KH_TT_{Guid.NewGuid():N}"[..10];
        int khId = await setupConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
            VALUES (@maKh, N'Khách Hàng Nợ Test TT', 50000000, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", new { maKh });

        string maHd = $"HD_TT_{Guid.NewGuid():N}"[..10];
        int hdId = await setupConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai)
            VALUES (@maHd, @khId, DATEADD(day, -5, GETDATE()), DATEADD(day, 25, GETDATE()), 10000000, 0, 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", new { maHd, khId });

        // Mở connection khác và chiếm giữ Exclusive AppLock 'QuanLyBanHang_CapMaPhieuThu'
        using var holdingConn = new SqlConnection(TestConnectionString);
        await holdingConn.OpenAsync();
        using var holdingTrans = holdingConn.BeginTransaction();

        const string sqlHoldLock = @"
            DECLARE @r INT;
            EXEC @r = sp_getapplock 
                @Resource = 'QuanLyBanHang_CapMaPhieuThu', 
                @LockMode = 'Exclusive', 
                @LockOwner = 'Transaction', 
                @LockTimeout = -1,
                @DbPrincipal = 'public';
            SELECT @r;";
        var holdResult = await holdingConn.ExecuteScalarAsync<int>(sqlHoldLock, transaction: holdingTrans);
        Assert.True(holdResult >= 0);

        try
        {
            var request = new TaoThanhToanRequest
            {
                KhachHangId = khId,
                NgayThanhToan = DateTime.Today,
                SoTien = 4000000,
                GhiChu = "Phiếu thu test timeout lock"
            };

            var result = await _thanhToanRepo.GhiNhanThanhToanAsync(request);

            Assert.False(result.ThanhCong);
            Assert.Contains("quá thời gian chờ", result.ThongBao);

            // Kiểm tra DB: Không có phiếu thu nào được chèn
            int countPt = await setupConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.ThanhToans WHERE KhachHangId = @khId;", new { khId });
            Assert.Equal(0, countPt);

            // Kiểm tra DB: Hóa đơn chưa thanh toán vẫn còn nguyên DaThanhToan = 0
            decimal daThanhToan = await setupConn.ExecuteScalarAsync<decimal>(
                "SELECT DaThanhToan FROM dbo.HoaDons WHERE HoaDonId = @hdId;", new { hdId });
            Assert.Equal(0, daThanhToan);

            // Kiểm tra DB: Không có phân bổ thanh toán nào được chèn
            int countPb = await setupConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.PhanBoThanhToans WHERE HoaDonId = @hdId;", new { hdId });
            Assert.Equal(0, countPb);
        }
        finally
        {
            holdingTrans.Rollback();
        }
    }
}
