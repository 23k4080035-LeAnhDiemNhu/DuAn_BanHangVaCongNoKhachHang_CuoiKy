using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanHang.DataStore.SQL;
using BanHang.UseCases.BaoCaoTuoiNoScreen;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.ThanhToanScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BanHang.Tests;

public class BaoCaoTuoiNoIntegrationTests : IAsyncLifetime
{
    private const string TestConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

    private readonly SqlConnectionFactory _connectionFactory;
    private readonly BaoCaoTuoiNoRepository _baoCaoRepo;
    private readonly XemBaoCaoTuoiNoUseCase _xemBaoCaoUseCase;
    private readonly HoaDonRepository _hoaDonRepo;
    private readonly TaoHoaDonBanChiuUseCase _taoHoaDonUseCase;
    private readonly ThanhToanRepository _thanhToanRepo;
    private readonly TaoThanhToanUseCase _taoThanhToanUseCase;

    private int KhachHang1Id;
    private int KhachHang2Id;
    private int SanPhamId;

    public BaoCaoTuoiNoIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        _baoCaoRepo = new BaoCaoTuoiNoRepository(_connectionFactory);
        _xemBaoCaoUseCase = new XemBaoCaoTuoiNoUseCase(_baoCaoRepo);

        _hoaDonRepo = new HoaDonRepository(_connectionFactory);
        _taoHoaDonUseCase = new TaoHoaDonBanChiuUseCase(_hoaDonRepo);

        _thanhToanRepo = new ThanhToanRepository(_connectionFactory);
        _taoThanhToanUseCase = new TaoThanhToanUseCase(_thanhToanRepo);
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
                    SoDienThoai NVARCHAR(20) NULL,
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

        // Seed khách hàng và sản phẩm
        KhachHang1Id = await testConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
            VALUES ('KH_BC1', N'Công ty Cổ phần Alpha', 50000000, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        KhachHang2Id = await testConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
            VALUES ('KH_BC2', N'Công ty TNHH Beta', 30000000, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");

        SanPhamId = await testConn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
            VALUES ('SP_BC1', N'Sản Phẩm Kiểm Thử', N'Bộ', 1000000, 500, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test01_BienNgay_HoaDonLapSauNgayBaoCao_KhongDuaVaoBaoCao()
    {
        // HD1 lập ngày 2026-10-16 (5M)
        var hd1Res = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_BC_01",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 10, 16),
            HanThanhToan = new DateTime(2026, 11, 15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto> { new() { SanPhamId = SanPhamId, SoLuong = 5, DonGiaHienThi = 1000000 } }
        });
        Assert.True(hd1Res.ThanhCong);

        // HD2 lập ngày 2026-10-17 (3M)
        var hd2Res = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_BC_02",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 10, 17),
            HanThanhToan = new DateTime(2026, 11, 16),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto> { new() { SanPhamId = SanPhamId, SoLuong = 3, DonGiaHienThi = 1000000 } }
        });
        Assert.True(hd2Res.ThanhCong);

        // Xem báo cáo tại ngày 2026-10-16 -> Chỉ tính HD_BC_01 (5M), bỏ qua HD_BC_02
        var report = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 16));
        var kh1 = report.DanhSach.First(x => x.KhachHangId == KhachHang1Id);

        Assert.Equal(5000000m, kh1.TongCongNo);
        Assert.Equal(5000000m, kh1.TuoiNo_0_30);
    }

    [Fact]
    public async Task Test02_ThanhToanVaPhanBoSauNgayBaoCao_KhongLamGiamNoQuaKhu()
    {
        // 1. Tạo hóa đơn ngày 2026-10-01 (6M, hạn 2026-10-20)
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_BC_TT1",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 10, 1),
            HanThanhToan = new DateTime(2026, 10, 20),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto> { new() { SanPhamId = SanPhamId, SoLuong = 6, DonGiaHienThi = 1000000 } }
        });
        Assert.True(hdRes.ThanhCong);

        // 2. Thu tiền vào ngày 2026-10-10 (4M)
        var ttRes = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_BC_01",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = new DateTime(2026, 10, 10),
            SoTien = 4000000m,
            PhuongThuc = "ChuyenKhoan"
        });
        Assert.True(ttRes.ThanhCong);

        // Cập nhật NgayPhanBo thành 2026-10-10 để mô phỏng giao dịch thực hiện vào ngày 10/10
        using (var conn = new SqlConnection(TestConnectionString))
        {
            await conn.ExecuteAsync(
                "UPDATE dbo.PhanBoThanhToans SET NgayPhanBo = '2026-10-10' WHERE ThanhToanId = @PtId;",
                new { PtId = ttRes.ThanhToanIdMoi!.Value });
        }

        // 3. Báo cáo tại ngày 2026-10-05 (trước ngày thu 10/10) -> Dư nợ vẫn là 6M nguyên vẹn
        var reportTruocThu = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 5));
        var kh1Truoc = reportTruocThu.DanhSach.First(x => x.KhachHangId == KhachHang1Id);
        Assert.Equal(6000000m, kh1Truoc.TongCongNo);

        // 4. Báo cáo tại ngày 2026-10-10 (sau ngày thu 10/10) -> Dư nợ giảm còn 2M
        var reportSauThu = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 10));
        var kh1Sau = reportSauThu.DanhSach.First(x => x.KhachHangId == KhachHang1Id);
        Assert.Equal(2000000m, kh1Sau.TongCongNo);
    }

    [Fact]
    public async Task Test03_PhieuThuLuiNgayNhungPhanBoSauNgayBaoCao_KhongLamGiamNoTaiNgayBaoCao()
    {
        // 1. Tạo hóa đơn ngày 2026-10-01 (10M)
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_LUI_01",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 10, 1),
            HanThanhToan = new DateTime(2026, 10, 15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto> { new() { SanPhamId = SanPhamId, SoLuong = 10, DonGiaHienThi = 1000000 } }
        });
        Assert.True(hdRes.ThanhCong);

        // 2. Insert thủ công phiếu thu có NgayThanhToan lùi về ngày 2026-10-02, nhưng NgayPhanBo là ngày 2026-10-10
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        var ptId = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, NgayTao)
            VALUES ('PT_LUI_MANUAL', @KhId, '2026-10-02', 4000000, N'TienMat', '2026-10-10');
            SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhId = KhachHang1Id });

        await conn.ExecuteAsync(@"
            INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
            VALUES (@PtId, @HdId, 4000000, '2026-10-10');
            UPDATE dbo.HoaDons SET DaThanhToan = 4000000, TrangThai = 1 WHERE HoaDonId = @HdId;",
            new { PtId = ptId, HdId = hdRes.HoaDonIdMoi!.Value });

        // 3. Quy ước: Báo cáo ngày 2026-10-05 yêu cầu cả NgayThanhToan và NgayPhanBo đều <= 2026-10-05.
        // Vì NgayPhanBo là 2026-10-10, nên tại ngày 2026-10-05 khoản phân bổ này chưa được ghi nhận trên hệ thống -> Dư nợ vẫn là 10M
        var reportNgay5 = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 5));
        var kh1Ngay5 = reportNgay5.DanhSach.First(x => x.KhachHangId == KhachHang1Id);
        Assert.Equal(10000000m, kh1Ngay5.TongCongNo);

        // 4. Báo cáo tại ngày 2026-10-10: Cả 2 điều kiện đều thỏa mãn -> Dư nợ là 6M
        var reportNgay10 = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 10));
        var kh1Ngay10 = reportNgay10.DanhSach.First(x => x.KhachHangId == KhachHang1Id);
        Assert.Equal(6000000m, kh1Ngay10.TongCongNo);
    }

    [Fact]
    public async Task Test04_BaoGomGiaoDichCuoiNgay_LoaiGiaoDichDauNgayKeTiep()
    {
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        // HD cuối ngày 2026-10-16 23:59:59 (4M)
        await conn.ExecuteAsync(@"
            INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai)
            VALUES ('HD_END_DAY', @KhId, '2026-10-16 23:59:59', '2026-11-15', 4000000, 0, 0);",
            new { KhId = KhachHang1Id });

        // HD đầu ngày hôm sau 2026-10-17 00:00:00 (7M)
        await conn.ExecuteAsync(@"
            INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai)
            VALUES ('HD_NEXT_DAY', @KhId, '2026-10-17 00:00:00', '2026-11-16', 7000000, 0, 0);",
            new { KhId = KhachHang1Id });

        var report = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 16));
        var kh1 = report.DanhSach.First(x => x.KhachHangId == KhachHang1Id);

        // Chỉ chứa HD_END_DAY (4M), không chứa HD_NEXT_DAY (7M)
        Assert.Equal(4000000m, kh1.TongCongNo);
    }

    [Fact]
    public async Task Test05_NhieuPhanBoTrenCungHoaDon_KhongNhanDoiTien_TongNhomBangTongNo()
    {
        // 1. Tạo 1 hóa đơn 10M
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_MULTI_PB",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 8, 1),
            HanThanhToan = new DateTime(2026, 8, 31), // Quá hạn 46 ngày so với 16/10 -> Nhóm 31-60
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto> { new() { SanPhamId = SanPhamId, SoLuong = 10, DonGiaHienThi = 1000000 } }
        });
        Assert.True(hdRes.ThanhCong);

        // 2. Thu lần 1: 2M
        var pt1 = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_M1",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = new DateTime(2026, 9, 1),
            SoTien = 2000000m,
            PhuongThuc = "TienMat"
        });
        Assert.True(pt1.ThanhCong);

        // 3. Thu lần 2: 3M
        var pt2 = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_M2",
            KhachHangId = KhachHang1Id,
            NgayThanhToan = new DateTime(2026, 9, 15),
            SoTien = 3000000m,
            PhuongThuc = "ChuyenKhoan"
        });
        Assert.True(pt2.ThanhCong);

        // 4. Báo cáo ngày 16/10: Hóa đơn 10M đã được 2 phân bổ (2M + 3M) = 5M -> Dư nợ còn 5M
        var report = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 16));
        var kh1 = report.DanhSach.First(x => x.KhachHangId == KhachHang1Id);

        // Kiểm tra CTE không làm nhân đôi số dòng/tiền
        Assert.Equal(5000000m, kh1.TongCongNo);
        Assert.Equal(5000000m, kh1.TuoiNo_31_60);
        Assert.Equal(0m, kh1.TuoiNo_0_30);
        Assert.Equal(0m, kh1.TuoiNo_61_90);
        Assert.Equal(0m, kh1.TuoiNo_Tren_90);

        // Kiểm tra tổng các nhóm bằng tổng công nợ
        Assert.Equal(kh1.TongCongNo, kh1.TuoiNo_0_30 + kh1.TuoiNo_31_60 + kh1.TuoiNo_61_90 + kh1.TuoiNo_Tren_90);
    }

    [Fact]
    public async Task Test06_KhachHangKhongCoNo_HienThiDayDuVaTongNoBang0()
    {
        // KH2 chưa có hóa đơn nào
        var report = await _xemBaoCaoUseCase.ExecuteAsync(new DateTime(2026, 10, 16));
        var kh2 = report.DanhSach.FirstOrDefault(x => x.KhachHangId == KhachHang2Id);

        Assert.NotNull(kh2);
        Assert.Equal(0m, kh2.TongCongNo);
        Assert.Equal(0m, kh2.TuoiNo_0_30);
        Assert.Equal(0m, kh2.TuoiNo_31_60);
        Assert.Equal(0m, kh2.TuoiNo_61_90);
        Assert.Equal(0m, kh2.TuoiNo_Tren_90);
    }

    [Fact]
    public async Task Test07_DocBaoCaoSerializable_ChanThanhToanXenKe_BaoDamKhongTronTrangThai()
    {
        // 1. Chuẩn bị dữ liệu ban đầu tại T0:
        // KH1 có 1 hóa đơn 10M (chưa thanh toán) và 1 phiếu thu 10M (chưa phân bổ).
        // Trạng thái T0: Dư nợ hóa đơn = 10M, Tiền chưa phân bổ = 10M.
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_SYNC_01",
            KhachHangId = KhachHang1Id,
            NgayLap = new DateTime(2026, 10, 1),
            HanThanhToan = new DateTime(2026, 10, 20),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto> { new() { SanPhamId = SanPhamId, SoLuong = 10, DonGiaHienThi = 1000000 } }
        });
        Assert.True(hdRes.ThanhCong);
        int hoaDonId = hdRes.HoaDonIdMoi!.Value;

        using var connSeed = new SqlConnection(TestConnectionString);
        await connSeed.OpenAsync();
        int ptId = await connSeed.ExecuteScalarAsync<int>(@"
            INSERT INTO dbo.ThanhToans (MaPhieuThu, KhachHangId, NgayThanhToan, SoTien, PhuongThuc, NgayTao)
            VALUES ('PT_SYNC_01', @KhId, '2026-10-05', 10000000, N'ChuyenKhoan', '2026-10-05');
            SELECT CAST(SCOPE_IDENTITY() AS INT);", new { KhId = KhachHang1Id });

        // 2. Thiết lập điều phối thời điểm thực thi bằng TaskCompletionSource
        var tcsSelect1Done = new TaskCompletionSource<bool>();
        var tcsTask2Attempted = new TaskCompletionSource<bool>();

        var ngayBaoCao = new DateTime(2026, 10, 16);
        var ngayKeTiep = ngayBaoCao.Date.AddDays(1);

        List<KhachHangHoaDonTuoiNoRawDto> rawDataResult = null!;
        decimal tienChuaPhanBoResult = -1;

        // 3. Luồng 1: Chạy đọc báo cáo trong transaction Serializable
        var reportTask = Task.Run(async () =>
        {
            using var connReport = new SqlConnection(TestConnectionString);
            await connReport.OpenAsync();
            using var transReport = connReport.BeginTransaction(System.Data.IsolationLevel.Serializable);

            const string sql1 = @"
                WITH PhanBoHopLe AS (
                    SELECT p.HoaDonId, SUM(p.SoTienPhanBo) AS TongPhanBo
                    FROM dbo.PhanBoThanhToans p
                    INNER JOIN dbo.ThanhToans t ON p.ThanhToanId = t.ThanhToanId
                    WHERE t.NgayThanhToan < @NgayKeTiep AND p.NgayPhanBo < @NgayKeTiep
                    GROUP BY p.HoaDonId
                )
                SELECT 
                    k.KhachHangId, k.MaKhachHang, k.TenKhachHang, k.SoDienThoai, k.HanMucCongNo,
                    h.HoaDonId, h.MaHoaDon, h.NgayLap, h.HanThanhToan, h.TongTien,
                    ISNULL(pb.TongPhanBo, 0) AS TongDaPhanBoCoHieuLuc
                FROM dbo.KhachHangs k
                LEFT JOIN dbo.HoaDons h ON k.KhachHangId = h.KhachHangId AND h.NgayLap < @NgayKeTiep
                LEFT JOIN PhanBoHopLe pb ON h.HoaDonId = pb.HoaDonId
                WHERE k.KhachHangId = @KhId
                ORDER BY k.KhachHangId, h.NgayLap, h.HoaDonId;";

            var raw1 = (await connReport.QueryAsync<KhachHangHoaDonTuoiNoRawDto>(
                sql1, new { NgayKeTiep = ngayKeTiep, KhId = KhachHang1Id }, transaction: transReport)).ToList();

            // Phát tín hiệu: SELECT 1 đã đọc xong trạng thái T0 (nợ 10M)!
            tcsSelect1Done.SetResult(true);

            // Chờ một khoảng thời gian ngắn để Luồng 2 cố gắng thực hiện ghi phân bổ xen vào giữa
            await tcsTask2Attempted.Task;
            await Task.Delay(200);

            const string sql2 = @"
                WITH PhanBoHopLePT AS (
                    SELECT p.ThanhToanId, SUM(p.SoTienPhanBo) AS TongDaPhanBo
                    FROM dbo.PhanBoThanhToans p
                    WHERE p.NgayPhanBo < @NgayKeTiep
                    GROUP BY p.ThanhToanId
                )
                SELECT ISNULL(SUM(
                    CASE 
                        WHEN (t.SoTien - ISNULL(pb.TongDaPhanBo, 0)) > 0 
                        THEN (t.SoTien - ISNULL(pb.TongDaPhanBo, 0)) 
                        ELSE 0 
                    END), 0)
                FROM dbo.ThanhToans t
                LEFT JOIN PhanBoHopLePT pb ON t.ThanhToanId = pb.ThanhToanId
                WHERE t.NgayThanhToan < @NgayKeTiep AND t.KhachHangId = @KhId;";

            var tien2 = await connReport.ExecuteScalarAsync<decimal>(
                sql2, new { NgayKeTiep = ngayKeTiep, KhId = KhachHang1Id }, transaction: transReport);

            transReport.Commit();

            rawDataResult = raw1;
            tienChuaPhanBoResult = tien2;
        });

        // 4. Luồng 2: Cố gắng chèn phân bổ (chuyển T0 -> T1) XEN VÀO GIỮA 2 câu SELECT
        var writerTask = Task.Run(async () =>
        {
            // Chờ cho đến khi Luồng 1 thực hiện xong SELECT 1
            await tcsSelect1Done.Task;

            using var connWriter = new SqlConnection(TestConnectionString);
            await connWriter.OpenAsync();

            // Báo cho Luồng 1 biết là Luồng 2 đang bắt đầu cố gắng ghi
            tcsTask2Attempted.SetResult(true);

            // Thực hiện ghi nhận phân bổ 10M cho hóa đơn
            // Do Luồng 1 đang giữ RangeS lock ở mức Serializable, câu lệnh này sẽ bị SQL Server chặn lại
            // cho đến khi Luồng 1 chạy xong SELECT 2 và commit transReport!
            using var transWriter = connWriter.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                await connWriter.ExecuteAsync(@"
                    INSERT INTO dbo.PhanBoThanhToans (ThanhToanId, HoaDonId, SoTienPhanBo, NgayPhanBo)
                    VALUES (@PtId, @HdId, 10000000, '2026-10-10');
                    UPDATE dbo.HoaDons SET DaThanhToan = 10000000, TrangThai = 2 WHERE HoaDonId = @HdId;",
                    new { PtId = ptId, HdId = hoaDonId },
                    transaction: transWriter);

                transWriter.Commit();
            }
            catch
            {
                transWriter.Rollback();
                throw;
            }
        });

        // 5. Chờ cả 2 task hoàn thành
        await Task.WhenAll(reportTask, writerTask);

        // 6. XÁC MINH:
        // Luồng 1 đọc được trạng thái T0 hoàn toàn nhất quán:
        // - Câu 1 đọc: Hóa đơn còn nợ 10M (chưa phân bổ)
        // - Câu 2 đọc: Tiền chưa phân bổ = 10M
        // Tuyệt đối không bị trộn: không có chuyện Câu 1 đọc nợ 10M còn Câu 2 bị Luồng 2 xen vào thành 0M!
        var hdDoc = rawDataResult.First(x => x.HoaDonId == hoaDonId);
        decimal duNoTaiNgay = hdDoc.TongTien!.Value - hdDoc.TongDaPhanBoCoHieuLuc!.Value;

        Assert.Equal(10000000m, duNoTaiNgay);
        Assert.Equal(10000000m, tienChuaPhanBoResult);

        // 7. Sau khi cả 2 đã commit xong, lần đọc báo cáo mới sẽ đọc được trạng thái T1 nhất quán:
        var reportSauCung = await _xemBaoCaoUseCase.ExecuteAsync(ngayBaoCao);
        var khSau = reportSauCung.DanhSach.First(x => x.KhachHangId == KhachHang1Id);

        Assert.Equal(0m, khSau.TongCongNo); // Đã thanh toán xong
        Assert.Equal(0m, reportSauCung.TienThuChuaPhanBo); // Tiền đã phân bổ hết
    }
}
