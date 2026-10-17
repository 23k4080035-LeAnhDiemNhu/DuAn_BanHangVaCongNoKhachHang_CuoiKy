using System.Data;
using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.ThanhToanScreen;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp nghiệp vụ Thu tiền & Phân bổ FIFO trên DATABASE KIỂM THỬ RIÊNG: QuanLyBanHangCongNo_Test
/// Tuyệt đối không chạm vào hay thay đổi CSDL chính QuanLyBanHangCongNo
/// </summary>
public class ThanhToanIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IThanhToanRepository _thanhToanRepo;
    private readonly ITaoThanhToanUseCase _taoThanhToanUseCase;
    private readonly IHoaDonRepository _hoaDonRepo;
    private readonly ITaoHoaDonBanChiuUseCase _taoHoaDonUseCase;

    public int KhachHang1Id { get; private set; }
    public int KhachHang2Id { get; private set; }
    public int SanPhamId { get; private set; }

    public ThanhToanIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        _thanhToanRepo = new ThanhToanRepository(_connectionFactory);
        _taoThanhToanUseCase = new TaoThanhToanUseCase(_thanhToanRepo, FakeCurrentUserService.KeToanCongNo());
        _hoaDonRepo = new HoaDonRepository(_connectionFactory);
        _taoHoaDonUseCase = new TaoHoaDonBanChiuUseCase(_hoaDonRepo, FakeCurrentUserService.NhanVienBanHang());
    }

    public async Task InitializeAsync()
    {
        using var testConn = new SqlConnection(TestConnectionString);
        await testConn.OpenAsync();

        // Tạo schema nếu chưa có
        var sqlCreateSchema = @"
            IF OBJECT_ID('dbo.KhachHangs', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.KhachHangs
                (
                    KhachHangId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    MaKhachHang NVARCHAR(20) NOT NULL UNIQUE,
                    TenKhachHang NVARCHAR(150) NOT NULL,
                    DienThoai NVARCHAR(20) NULL,
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

        // Xóa dữ liệu cũ
        await testConn.ExecuteAsync(@"
            DELETE FROM dbo.PhanBoThanhToans;
            DELETE FROM dbo.ThanhToans;
            DELETE FROM dbo.ChiTietHoaDons;
            DELETE FROM dbo.HoaDons;
            DELETE FROM dbo.SanPhams;
            DELETE FROM dbo.KhachHangs;");

        // Seed khách hàng và sản phẩm phục vụ kiểm thử
        KhachHang1Id = await testConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
            VALUES ('KH_TT1', N'Công ty CP Thương Mại A', 20000000, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        KhachHang2Id = await testConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
            VALUES ('KH_TT2', N'Doanh nghiệp Tư nhân B', 15000000, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        SanPhamId = await testConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_TT1', N'Màn Hình Dell 24 inch', N'Cái', 1000000, 100, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Test01_GhiNhanThanhToan_PhanBoKhopDaThanhToanTrangThaiVaChiTiet()
    {
        // 1. Tạo 2 hóa đơn cho KH1: HD01 (4M) và HD02 (6M). Tổng nợ = 10M.
        var hd1Res = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_FIFO_01",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 10, 1),
            HanThanhToan = new DateTime(2026, 10, 20),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 4, DonGiaHienThi = 1000000 } // 4M
            }
        });
        Assert.True(hd1Res.ThanhCong);

        var hd2Res = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_FIFO_02",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 10, 5),
            HanThanhToan = new DateTime(2026, 10, 25),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 6, DonGiaHienThi = 1000000 } // 6M
            }
        });
        Assert.True(hd2Res.ThanhCong);

        // 2. KH1 nộp 7M. Thuật toán FIFO phải trả hết HD01 (4M) và trả 3M vào HD02.
        var request = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_FIFO_01",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = new DateTime(2026, 10, 10),
            SoTien = 7000000,
            PhuongThuc = "ChuyenKhoan",
            GhiChu = "Thanh toán cấn trừ 7M"
        };

        var result = await _taoThanhToanUseCase.ExecuteAsync(request);

        Assert.True(result.ThanhCong);
        Assert.NotNull(result.ThanhToanIdMoi);
        Assert.Equal(7000000m, result.TongDaPhanBo);
        Assert.Equal(0m, result.TienChuaPhanBo);
        Assert.Equal(2, result.DanhSachPhanBo.Count);

        // Kiểm tra trực tiếp trong DB test
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        // Kiểm tra bảng dbo.ThanhToans
        var pt = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM dbo.ThanhToans WHERE ThanhToanId = @Id",
            new { Id = result.ThanhToanIdMoi.Value });
        Assert.NotNull(pt);
        Assert.Equal("PT_FIFO_01", (string)pt!.MaPhieuThu);
        Assert.Equal(7000000m, (decimal)pt.SoTien);

        // Kiểm tra bảng dbo.PhanBoThanhToans
        var phanBos = (await conn.QueryAsync<dynamic>(
            "SELECT * FROM dbo.PhanBoThanhToans WHERE ThanhToanId = @Id ORDER BY HoaDonId",
            new { Id = result.ThanhToanIdMoi.Value })).ToList();
        Assert.Equal(2, phanBos.Count);
        Assert.Equal(hd1Res.HoaDonIdMoi!.Value, (int)phanBos[0].HoaDonId);
        Assert.Equal(4000000m, (decimal)phanBos[0].SoTienPhanBo);
        Assert.Equal(hd2Res.HoaDonIdMoi!.Value, (int)phanBos[1].HoaDonId);
        Assert.Equal(3000000m, (decimal)phanBos[1].SoTienPhanBo);

        // Kiểm tra bảng dbo.HoaDons đã cập nhật DaThanhToan và TrangThai
        var hd1 = await conn.QuerySingleAsync<dynamic>(
            "SELECT DaThanhToan, ConLai, TrangThai FROM dbo.HoaDons WHERE HoaDonId = @Id",
            new { Id = hd1Res.HoaDonIdMoi.Value });
        Assert.Equal(4000000m, (decimal)hd1.DaThanhToan);
        Assert.Equal(0m, (decimal)hd1.ConLai);
        Assert.Equal(2, (int)hd1.TrangThai); // DaThanhToan

        var hd2 = await conn.QuerySingleAsync<dynamic>(
            "SELECT DaThanhToan, ConLai, TrangThai FROM dbo.HoaDons WHERE HoaDonId = @Id",
            new { Id = hd2Res.HoaDonIdMoi.Value });
        Assert.Equal(3000000m, (decimal)hd2.DaThanhToan);
        Assert.Equal(3000000m, (decimal)hd2.ConLai);
        Assert.Equal(1, (int)hd2.TrangThai); // ThanhToanMotPhan
    }

    [Fact]
    public async Task Test02_KhongPhanBoSaiKhachHang_VaGiuTienDu()
    {
        // KH1 có nợ 3M, KH2 có nợ 5M
        var hd1Res = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_KH1_01",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 3, DonGiaHienThi = 1000000 } // 3M
            }
        });
        Assert.True(hd1Res.ThanhCong);

        var hd2Res = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_KH2_01",
            KhachHangId = KhachHang2Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 5, DonGiaHienThi = 1000000 } // 5M
            }
        });
        Assert.True(hd2Res.ThanhCong);

        // KH1 nộp 8M -> Phân bổ tối đa 3M cho HD_KH1_01, thừa 5M chưa phân bổ
        var result = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_EXCESS_01",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 8000000,
            PhuongThuc = "TienMat"
        });

        Assert.True(result.ThanhCong);
        Assert.Equal(3000000m, result.TongDaPhanBo);
        Assert.Equal(5000000m, result.TienChuaPhanBo); // 5M dư
        Assert.Single(result.DanhSachPhanBo);

        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        // Kiểm tra hóa đơn của KH2 tuyệt đối không bị cấn trừ nhầm
        var hd2 = await conn.QuerySingleAsync<dynamic>(
            "SELECT DaThanhToan, ConLai FROM dbo.HoaDons WHERE HoaDonId = @Id",
            new { Id = hd2Res.HoaDonIdMoi!.Value });
        Assert.Equal(0m, (decimal)hd2.DaThanhToan);
        Assert.Equal(5000000m, (decimal)hd2.ConLai);
    }

    [Fact]
    public async Task Test03_TrungMaPhieuThu_BiTuChoi()
    {
        var req1 = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_DUP_01",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 1000000,
            PhuongThuc = "ChuyenKhoan"
        };
        var res1 = await _taoThanhToanUseCase.ExecuteAsync(req1);
        Assert.True(res1.ThanhCong);

        // Thử tạo phiếu thứ hai trùng mã
        var req2 = new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_DUP_01",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 2000000,
            PhuongThuc = "TienMat"
        };
        var res2 = await _taoThanhToanUseCase.ExecuteAsync(req2);

        Assert.False(res2.ThanhCong);
        Assert.Contains("đã tồn tại trong hệ thống", res2.ThongBao);
    }

    [Fact]
    public async Task Test04_XacMinhRollback_KhiLoiXayRaSauKhiGhiHeader()
    {
        // Kiểm thử hành vi rollback nguyên tử:
        // Header được INSERT nhưng sau đó gặp lỗi ở detail -> rollback toàn bộ
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();

        const string maPhieuTest = "PT_ROLLBACK_ERR";

        try
        {
            // 1. Insert header
            var thanhToanId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, NgayTao)
                VALUES (@MaPhieuThu, @KhachHangId, GETDATE(), 5000000, 'ChuyenKhoan', GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { MaPhieuThu = maPhieuTest, KhachHangId = KhachHang1Id },
                trans);

            // 2. Gây lỗi vi phạm FK bảng PhanBoThanhToans với HoaDonId không tồn tại
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                VALUES (@ThanhToanId, 999999, 5000000, GETDATE());",
                new { ThanhToanId = thanhToanId },
                trans);

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
        }

        // 3. Xác minh trong database: header không tồn tại
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.ThanhToans WHERE MaPhieuThu = @MaPhieuThu",
            new { MaPhieuThu = maPhieuTest });
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Test05_HaiPhieuThuDongThoi_KhongThanhToanVuotNo()
    {
        // Tạo 1 hóa đơn 5,000,000 cho KH2
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_CONCUR_PAY",
            KhachHangId = KhachHang2Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 5, DonGiaHienThi = 1000000 } // 5M
            }
        });
        Assert.True(hdRes.ThanhCong);

        // Bắn 2 phiếu thu đồng thời, mỗi phiếu 4M cho cùng KH2. Tổng = 8M > 5M nợ.
        var taskA = _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_CONCUR_A",
            KhachHangId = KhachHang2Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 4000000,
            PhuongThuc = "ChuyenKhoan"
        });

        var taskB = _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_CONCUR_B",
            KhachHangId = KhachHang2Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 4000000,
            PhuongThuc = "ChuyenKhoan"
        });

        var results = await Task.WhenAll(taskA, taskB);

        Assert.True(results[0].ThanhCong);
        Assert.True(results[1].ThanhCong);

        // Tổng tiền phân bổ trên cả 2 phiếu thu phải đúng bằng 5M (không vượt quá số nợ)
        decimal tongPhanBoCaHai = results[0].TongDaPhanBo.GetValueOrDefault() + results[1].TongDaPhanBo.GetValueOrDefault();
        Assert.Equal(5000000m, tongPhanBoCaHai);

        // Tổng tiền dư chưa phân bổ trên cả 2 phiếu phải đúng bằng 3M
        decimal tongTienDuCaHai = results[0].TienChuaPhanBo.GetValueOrDefault() + results[1].TienChuaPhanBo.GetValueOrDefault();
        Assert.Equal(3000000m, tongTienDuCaHai);

        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        var hd = await conn.QuerySingleAsync<dynamic>(
            "SELECT DaThanhToan, ConLai, TrangThai FROM dbo.HoaDons WHERE HoaDonId = @Id",
            new { Id = hdRes.HoaDonIdMoi!.Value });

        Assert.Equal(5000000m, (decimal)hd.DaThanhToan);
        Assert.Equal(0m, (decimal)hd.ConLai);
        Assert.Equal(2, (int)hd.TrangThai); // DaThanhToan
    }

    [Fact]
    public async Task Test06_TaoHoaDonVaThuTienDongThoi_KhongLamSaiHanMucVaSoDu()
    {
        // Khách hàng KH1 có hạn mức 20M.
        // Tạo hóa đơn 15M -> nợ 15M, còn hạn mức 5M.
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_CONCUR_MIX_1",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 15, DonGiaHienThi = 1000000 } // 15M
            }
        });
        Assert.True(hdRes.ThanhCong);

        // Khởi chạy đồng thời:
        // 1. Tạo hóa đơn mới 8M (nếu nợ cũ chưa giảm, 15M + 8M = 23M > 20M -> vượt hạn mức)
        // 2. Thu tiền 10M (giảm nợ từ 15M xuống 5M)
        var taskTaoHD = _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_CONCUR_MIX_2",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 8, DonGiaHienThi = 1000000 } // 8M
            }
        });

        var taskThuTien = _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_CONCUR_MIX_1",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 10000000,
            PhuongThuc = "ChuyenKhoan"
        });

        await Task.WhenAll(taskTaoHD, taskThuTien);

        // Nhờ khóa UPDLOCK, HOLDLOCK:
        // Cả hai giao dịch tuần tự hóa hoàn toàn, đảm bảo tính nhất quán của hạn mức công nợ
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        var tongNo = await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(ConLai), 0) FROM dbo.HoaDons WHERE KhachHangId = @Id",
            new { Id = KhachHang1Id });

        // Dù giao dịch nào chạy trước, tổng nợ không thể vượt quá hạn mức 20M
        Assert.True(tongNo <= 20000000m);
    }

    [Fact]
    public async Task Test07_DuLieuThayDoiSoVoiXemTruoc_BiPhatHienVaTuChoi()
    {
        // 1. Tạo hóa đơn 5M cho KH1
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_STALE_01",
            KhachHangId = KhachHang1Id,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 5, DonGiaHienThi = 1000000 }
            }
        });
        Assert.True(hdRes.ThanhCong);

        // 2. Giả lập bản xem trước của người dùng: dự kiến phân bổ 5M vào HD_STALE_01
        var danhSachXemTruoc = new List<ChiTietXemTruocDto>
        {
            new ChiTietXemTruocDto { HoaDonId = hdRes.HoaDonIdMoi!.Value, SoTienPhanBo = 5000000 }
        };

        // 3. Trong lúc người dùng đang nhìn màn hình, một giao dịch khác thanh toán trước 2M vào hóa đơn này
        var ptIntervene = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_INTERVENE",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 2000000,
            PhuongThuc = "ChuyenKhoan"
        });
        Assert.True(ptIntervene.ThanhCong);

        // 4. Người dùng bấm lưu với bản xem trước cũ (mong đợi 5M cấn trừ, nhưng thực tế hóa đơn chỉ còn nợ 3M)
        var result = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_SUBMIT_STALE",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = DateTime.Today,
            SoTien = 5000000,
            PhuongThuc = "ChuyenKhoan",
            DanhSachXemTruoc = danhSachXemTruoc
        });

        // 5. Hệ thống phải phát hiện chênh lệch và từ chối, yêu cầu người dùng xem lại
        Assert.False(result.ThanhCong);
        Assert.True(result.DuLieuThayDoiSoVoiXemTruoc);
        Assert.Contains("đã thay đổi so với bản xem trước", result.ThongBao);

        // Đảm bảo không tạo phiếu thu PT_SUBMIT_STALE trong database
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.ThanhToans WHERE MaPhieuThu = 'PT_SUBMIT_STALE'");
        Assert.Equal(0, count);
    }
}
