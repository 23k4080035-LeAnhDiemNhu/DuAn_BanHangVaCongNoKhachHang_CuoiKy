using System.Data;
using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp trên DATABASE KIỂM THỬ RIÊNG: QuanLyBanHangCongNo_Test
/// Tuyệt đối không chạm vào hay chỉnh sửa database chính QuanLyBanHangCongNo
/// </summary>
public class IntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IHoaDonRepository _hoaDonRepository;
    private readonly ITaoHoaDonBanChiuUseCase _useCase;

    public int KhachHang1Id { get; private set; }
    public int KhachHangInactiveId { get; private set; }
    public int KhachHangConcurId { get; private set; }
    public int SanPham1Id { get; private set; }
    public int SanPham2Id { get; private set; }
    public int SanPhamInactiveId { get; private set; }

    public IntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        _hoaDonRepository = new HoaDonRepository(_connectionFactory);
        _useCase = new TaoHoaDonBanChiuUseCase(_hoaDonRepository);
    }

    public async Task InitializeAsync()
    {
        // 1. Tạo database kiểm thử riêng QuanLyBanHangCongNo_Test nếu chưa có
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

        // 2. Tạo schema trên QuanLyBanHangCongNo_Test
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

            // Làm sạch dữ liệu trong database test trước mỗi lần chạy
            await testConn.ExecuteAsync(@"
                DELETE FROM dbo.PhanBoThanhToans;
                DELETE FROM dbo.ThanhToans;
                DELETE FROM dbo.ChiTietHoaDons;
                DELETE FROM dbo.HoaDons;
                DELETE FROM dbo.SanPhams;
                DELETE FROM dbo.KhachHangs;");

            // Seed dữ liệu mẫu riêng biệt cho test
            KhachHang1Id = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_TEST1', N'Công ty TNHH Thử Nghiệm 1', 10000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            KhachHangInactiveId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_INACTIVE', N'Khách Hàng Tạm Khóa', 5000000, 0);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            KhachHangConcurId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_CONCUR', N'Khách Hàng Kiểm Thử Đồng Thời', 5000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            SanPham1Id = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                VALUES ('SP_TEST1', N'Bàn Phím Cơ Gaming', N'Cái', 1000000, 100, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            SanPham2Id = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                VALUES ('SP_TEST2', N'Chuột Không Dây Pro', N'Cái', 500000, 100, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            SanPhamInactiveId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                VALUES ('SP_INACTIVE', N'Tai Nghe Cũ Ngừng Bán', N'Cái', 300000, 50, 0);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Test01_LuuHopLe_KhopHeaderVaDetail_VaGiaTriKhoiTao()
    {
        // Khách hàng có hạn mức 10M, công nợ hiện tại 0đ
        // Mua: 3 SP1 (3 x 1M = 3M) + 2 SP2 (2 x 500k = 1M) => Tổng 4M <= 10M
        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_VAL_01",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            GhiChu = "Đơn bán chịu thử nghiệm",
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham1Id, SoLuong = 3, DonGiaHienThi = 1000000 },
                new TaoChiTietHoaDonDto { SanPhamId = SanPham2Id, SoLuong = 2, DonGiaHienThi = 500000 }
            }
        };

        var result = await _useCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(result.HoaDonIdMoi);
        Assert.Equal(4000000m, result.TongTien);

        // Kiểm tra trực tiếp trong Database test
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        var hd = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM dbo.HoaDons WHERE HoaDonId = @HoaDonId",
            new { HoaDonId = result.HoaDonIdMoi.Value });

        Assert.NotNull(hd);
        Assert.Equal("HD_VAL_01", (string)hd!.MaHoaDon);
        Assert.Equal(4000000m, (decimal)hd.TongTien);
        Assert.Equal(0m, (decimal)hd.DaThanhToan);
        Assert.Equal(4000000m, (decimal)hd.ConLai);
        Assert.Equal(0, (int)hd.TrangThai); // ChuaThanhToan

        var chiTiets = (await conn.QueryAsync<dynamic>(
            "SELECT * FROM dbo.ChiTietHoaDons WHERE HoaDonId = @HoaDonId ORDER BY SanPhamId",
            new { HoaDonId = result.HoaDonIdMoi.Value })).ToList();

        Assert.Equal(2, chiTiets.Count);
        // SP1
        Assert.Equal("Bàn Phím Cơ Gaming", (string)chiTiets[0].TenSanPham);
        Assert.Equal("Cái", (string)chiTiets[0].DonViTinh);
        Assert.Equal(3, (int)chiTiets[0].SoLuong);
        Assert.Equal(1000000m, (decimal)chiTiets[0].DonGia);
        Assert.Equal(3000000m, (decimal)chiTiets[0].ThanhTien);

        // SP2
        Assert.Equal("Chuột Không Dây Pro", (string)chiTiets[1].TenSanPham);
        Assert.Equal("Cái", (string)chiTiets[1].DonViTinh);
        Assert.Equal(2, (int)chiTiets[1].SoLuong);
        Assert.Equal(500000m, (decimal)chiTiets[1].DonGia);
        Assert.Equal(1000000m, (decimal)chiTiets[1].ThanhTien);
    }

    [Fact]
    public async Task Test02_VuotHanMuc_BiTuChoi_VaKhongDeLaiDuLieu()
    {
        // Khách hàng KH_TEST1 có hạn mức 10M
        // Tạo đơn hàng 11 * 1M = 11M > 10M
        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_EXCEED_01",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham1Id, SoLuong = 11, DonGiaHienThi = 1000000 }
            }
        };

        var result = await _useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("vượt quá hạn mức công nợ", result.ThongBao);

        // Kiểm tra không để lại dữ liệu rác trong database
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        var countHd = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.HoaDons WHERE MaHoaDon = 'HD_EXCEED_01'");
        Assert.Equal(0, countHd);
    }

    [Fact]
    public async Task Test03_TrungMaHoaDon_BiTuChoi()
    {
        // Tạo hóa đơn đầu tiên
        var req1 = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_DUP_TEST",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham1Id, SoLuong = 1, DonGiaHienThi = 1000000 }
            }
        };
        var res1 = await _useCase.ExecuteAsync(req1);
        Assert.True(res1.ThanhCong);

        // Tạo hóa đơn thứ hai trùng mã
        var req2 = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_DUP_TEST",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham2Id, SoLuong = 1, DonGiaHienThi = 500000 }
            }
        };
        var res2 = await _useCase.ExecuteAsync(req2);

        Assert.False(res2.ThanhCong);
        Assert.Contains("đã tồn tại trong hệ thống", res2.ThongBao);
    }

    [Fact]
    public async Task Test04_KhachHangNgungHoatDong_BiTuChoi()
    {
        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_INACTIVE_CUST",
            KhachHangId = KhachHangInactiveId, // Khách hàng tạm khóa
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham1Id, SoLuong = 1, DonGiaHienThi = 1000000 }
            }
        };

        var result = await _useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("tạm khóa hoặc ngừng hoạt động", result.ThongBao);
    }

    [Fact]
    public async Task Test05_SanPhamNgungHoatDong_BiTuChoi()
    {
        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_INACTIVE_PROD",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamInactiveId, SoLuong = 1, DonGiaHienThi = 300000 }
            }
        };

        var result = await _useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.Contains("ngừng kinh doanh", result.ThongBao);
    }

    [Fact]
    public async Task Test06_XacMinhRollback_KhiLoiXayRaSauKhiGhiHeader()
    {
        // Kiểm thử hành vi rollback nguyên tử:
        // Cố tình thực hiện transaction trong đó header được INSERT nhưng sau đó gặp lỗi
        // Xác minh toàn bộ header và detail bị rollback, không để lại dữ liệu
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        const string maHoaDonTestRollback = "HD_TEST_ROLLBACK_ERR";

        try
        {
            // 1. Insert header
            var hoaDonId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.HoaDons 
                    (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai, GhiChu, NgayTao)
                VALUES 
                    (@MaHoaDon, @KhachHangId, GETDATE(), GETDATE() + 10, 1000000, 0, 0, 'Test rollback', GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { MaHoaDon = maHoaDonTestRollback, KhachHangId = KhachHang1Id },
                trans);

            // 2. Cố tình gây lỗi vi phạm khóa ngoại ở dòng detail
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.ChiTietHoaDons 
                    (HoaDonId, SanPhamId, TenSanPham, DonViTinh, SoLuong, DonGia)
                VALUES 
                    (@HoaDonId, 999999, 'SP Khong Ton Tai', 'Cai', 1, 1000000);",
                new { HoaDonId = hoaDonId },
                trans);

            trans.Commit();
        }
        catch
        {
            // Bắt lỗi và rollback
            trans.Rollback();
        }

        // 3. Xác minh trong database: header không tồn tại
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.HoaDons WHERE MaHoaDon = @MaHoaDon",
            new { MaHoaDon = maHoaDonTestRollback });

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Test07_HaiYeuCauDongThoiCungKhachHang_ToiDaMotYeuCauThanhCong()
    {
        // Khách hàng KH_CONCUR có hạn mức 5,000,000 và chưa nợ đồng nào.
        // Chuẩn bị 2 yêu cầu đồng thời:
        // Yêu cầu A: trị giá 3,000,000 (độc lập: 3M <= 5M -> Hợp lệ)
        // Yêu cầu B: trị giá 3,000,000 (độc lập: 3M <= 5M -> Hợp lệ)
        // Tổng cả hai: 6,000,000 > 5,000,000 (vượt hạn mức nếu cả hai cùng lọt qua).
        // Nhờ khóa UPDLOCK, HOLDLOCK, chỉ duy nhất 1 yêu cầu được thành công.
        var reqA = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_CONCUR_01",
            KhachHangId = KhachHangConcurId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham1Id, SoLuong = 3, DonGiaHienThi = 1000000 } // 3M
            }
        };

        var reqB = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_CONCUR_02",
            KhachHangId = KhachHangConcurId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham1Id, SoLuong = 3, DonGiaHienThi = 1000000 } // 3M
            }
        };

        // Bắn 2 yêu cầu đồng thời
        var taskA = _useCase.ExecuteAsync(reqA);
        var taskB = _useCase.ExecuteAsync(reqB);

        var results = await Task.WhenAll(taskA, taskB);

        var thanhCongCount = results.Count(r => r.ThanhCong);
        var thatBaiCount = results.Count(r => !r.ThanhCong);

        // Xác minh: Đúng 1 yêu cầu thành công, 1 yêu cầu thất bại do vượt hạn mức
        Assert.Equal(1, thanhCongCount);
        Assert.Equal(1, thatBaiCount);

        var failedResult = results.First(r => !r.ThanhCong);
        Assert.Contains("vượt quá hạn mức công nợ", failedResult.ThongBao);

        // Kiểm tra database: chỉ có đúng 1 hóa đơn được lưu cho KhachHangConcurId
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        var danhSachHd = (await conn.QueryAsync<dynamic>(
            "SELECT * FROM dbo.HoaDons WHERE KhachHangId = @KhachHangId",
            new { KhachHangId = KhachHangConcurId })).ToList();

        Assert.Single(danhSachHd);
        Assert.Equal(3000000m, (decimal)danhSachHd[0].TongTien);
    }

    [Fact]
    public async Task Test08_GiaBiThayDoiTrenHeThong_BiTuChoi_VaBaoGiaThayDoi()
    {
        // Giả lập tình huống: Khi người dùng mở màn hình, sản phẩm SP1 có giá 1,000,000.
        // Nhưng khi bấm lưu, DonGiaHienThi gửi lên là 900,000 (hoặc phía server giá đã đổi khác 900,000).
        var request = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_PRICE_CHANGE",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPham1Id, SoLuong = 2, DonGiaHienThi = 900000 } // Khác giá DB 1,000,000
            }
        };

        var result = await _useCase.ExecuteAsync(request);

        Assert.False(result.ThanhCong);
        Assert.True(result.GiaBiThayDoi);
        Assert.Contains("đã thay đổi", result.ThongBao);

        // Đảm bảo không tạo hóa đơn rác
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.HoaDons WHERE MaHoaDon = 'HD_PRICE_CHANGE'");
        Assert.Equal(0, count);
    }
}
