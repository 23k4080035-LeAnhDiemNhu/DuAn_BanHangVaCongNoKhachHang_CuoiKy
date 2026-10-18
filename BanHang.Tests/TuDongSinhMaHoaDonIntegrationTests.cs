using System.Data;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp cơ chế tự động cấp mã hóa đơn (HDxxx) và đồng thời (concurrency)
/// CHỈ chạy trên database kiểm thử riêng: QuanLyBanHangCongNo_Test
/// </summary>
public class TuDongSinhMaHoaDonIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IHoaDonRepository _hoaDonRepository;
    private readonly ITaoHoaDonBanChiuUseCase _useCase;

    public int KhachHangAId { get; private set; }
    public int KhachHangBId { get; private set; }
    public int SanPhamId { get; private set; }

    public TuDongSinhMaHoaDonIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        _hoaDonRepository = new HoaDonRepository(_connectionFactory);
        _useCase = new TaoHoaDonBanChiuUseCase(_hoaDonRepository, FakeCurrentUserService.NhanVienBanHang());
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

            // Làm sạch dữ liệu để test độc lập
            await testConn.ExecuteAsync(@"
                DELETE FROM dbo.PhanBoThanhToans;
                DELETE FROM dbo.ThanhToans;
                DELETE FROM dbo.ChiTietHoaDons;
                DELETE FROM dbo.HoaDons;
                DELETE FROM dbo.SanPhams;
                DELETE FROM dbo.KhachHangs;");

            KhachHangAId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_AUTO_A', N'Khách Hàng Auto A', 50000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            KhachHangBId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_AUTO_B', N'Khách Hàng Auto B', 50000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            SanPhamId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                VALUES ('SP_AUTO_1', N'Sản Phẩm Auto Test', N'Cái', 100000, 500, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test01_MaSinhTheoDuLieuHienCo_KhongDoiMaCu()
    {
        using (var conn = new SqlConnection(TestConnectionString))
        {
            await conn.OpenAsync();
            // Chèn dữ liệu mẫu có sẵn: HD001, HD002, HD010 và một mã đặc biệt không chuẩn HD_OLD_SPECIAL
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.HoaDons (MaHoaDon, KhachHangId, NgayLap, HanThanhToan, TongTien, DaThanhToan, TrangThai)
                VALUES 
                    ('HD001', @KhA, GETDATE(), DATEADD(day, 30, GETDATE()), 100000, 0, 0),
                    ('HD002', @KhA, GETDATE(), DATEADD(day, 30, GETDATE()), 100000, 0, 0),
                    ('HD010', @KhA, GETDATE(), DATEADD(day, 30, GETDATE()), 100000, 0, 0),
                    ('HD_OLD_SPECIAL', @KhA, GETDATE(), DATEADD(day, 30, GETDATE()), 100000, 0, 0);",
                new { KhA = KhachHangAId });
        }

        // 1. Kiểm tra lấy mã tiếp theo: sau HD010 phải là HD011
        var nextCode = await _hoaDonRepository.LayMaHoaDonTiepTheoAsync();
        Assert.Equal("HD011", nextCode);

        // 2. Đảm bảo mã cũ và mã đặc biệt không bị thay đổi hoặc mất đi
        using (var conn = new SqlConnection(TestConnectionString))
        {
            await conn.OpenAsync();
            var existingCodes = (await conn.QueryAsync<string>("SELECT MaHoaDon FROM dbo.HoaDons ORDER BY HoaDonId")).ToList();
            Assert.Contains("HD001", existingCodes);
            Assert.Contains("HD002", existingCodes);
            Assert.Contains("HD010", existingCodes);
            Assert.Contains("HD_OLD_SPECIAL", existingCodes);
        }
    }

    [Fact]
    public async Task Test02_KhongChenHoaDonRong_KhiChiXemMaTiepTheo()
    {
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        var countBefore = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.HoaDons;");

        // Gọi lấy mã dự kiến 5 lần liên tiếp
        for (int i = 0; i < 5; i++)
        {
            var code = await _hoaDonRepository.LayMaHoaDonTiepTheoAsync();
            Assert.StartsWith("HD", code);
        }

        var countAfter = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.HoaDons;");
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task Test03_NhieuLanTaoLienTiep_KhongTrungMa()
    {
        // Tạo liên tiếp 3 hóa đơn không truyền mã thủ công
        var results = new List<TaoHoaDonResult>();
        for (int i = 0; i < 3; i++)
        {
            var req = new TaoHoaDonRequest
            {
                KhachHangId = KhachHangAId,
                MaHoaDon = string.Empty, // Hệ thống tự cấp
                NgayLap = DateTime.Today,
                HanThanhToan = DateTime.Today.AddDays(15),
                ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
                {
                    new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 1, DonGiaHienThi = 100000 }
                }
            };
            var res = await _useCase.ExecuteAsync(req);
            Assert.True(res.ThanhCong, res.ThongBao);
            results.Add(res);
        }

        // Kiểm tra các mã trả về khác nhau
        var codes = results.Select(r => r.MaHoaDonMoi).ToList();
        Assert.Equal(3, codes.Distinct().Count());
        Assert.Equal("HD001", codes[0]);
        Assert.Equal("HD002", codes[1]);
        Assert.Equal("HD003", codes[2]);

        // Kiểm tra trong database
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        var dbCodes = (await conn.QueryAsync<string>("SELECT MaHoaDon FROM dbo.HoaDons ORDER BY HoaDonId")).ToList();
        Assert.Equal(new[] { "HD001", "HD002", "HD003" }, dbCodes);
    }

    [Fact]
    public async Task Test04_HaiYeuCauDongThoi_TrenHaiKhachHangKhacNhau_NhanHaiMaKhacNhau_KhongDeadlock()
    {
        var task1 = _useCase.ExecuteAsync(new TaoHoaDonRequest
        {
            KhachHangId = KhachHangAId,
            MaHoaDon = string.Empty,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 1, DonGiaHienThi = 100000 }
            }
        });

        var task2 = _useCase.ExecuteAsync(new TaoHoaDonRequest
        {
            KhachHangId = KhachHangBId,
            MaHoaDon = string.Empty,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 2, DonGiaHienThi = 100000 }
            }
        });

        var results = await Task.WhenAll(task1, task2);

        Assert.True(results[0].ThanhCong, results[0].ThongBao);
        Assert.True(results[1].ThanhCong, results[1].ThongBao);

        // Phải nhận được hai mã khác nhau
        Assert.NotEqual(results[0].MaHoaDonMoi, results[1].MaHoaDonMoi);

        // Cả hai mã phải bắt đầu bằng HD
        Assert.StartsWith("HD", results[0].MaHoaDonMoi);
        Assert.StartsWith("HD", results[1].MaHoaDonMoi);

        // Đảm bảo cả hai hóa đơn đều có trong database
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        var totalInvoices = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.HoaDons;");
        Assert.Equal(2, totalInvoices);
    }

    [Fact]
    public async Task Test05_MaHienThiCu_KhongGayLoiLuu_HoacGhiDeHoaDon()
    {
        // Giả sử phiên 1 đã lưu trước và chiếm mã HD001
        var req1 = new TaoHoaDonRequest
        {
            KhachHangId = KhachHangAId,
            MaHoaDon = "HD001",
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 1, DonGiaHienThi = 100000 }
            }
        };
        var res1 = await _useCase.ExecuteAsync(req1);
        Assert.True(res1.ThanhCong);
        Assert.Equal("HD001", res1.MaHoaDonMoi);

        // Phiên 2 mở form cùng lúc thấy HD001, sau đó bấm Lưu với MaHoaDon="HD001"
        var req2 = new TaoHoaDonRequest
        {
            KhachHangId = KhachHangBId,
            MaHoaDon = "HD001", // Mã preview cũ đã bị chiếm
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamId, SoLuong = 3, DonGiaHienThi = 100000 }
            }
        };

        var res2 = await _useCase.ExecuteAsync(req2);
        // Lưu phải thành công, không bị lỗi duplicate key, không ghi đè HD001
        Assert.True(res2.ThanhCong, res2.ThongBao);
        Assert.Equal("HD002", res2.MaHoaDonMoi);

        // Kiểm tra database có đủ cả 2 hóa đơn riêng biệt
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();
        var invoices = (await conn.QueryAsync<(string MaHoaDon, int KhachHangId)>("SELECT MaHoaDon, KhachHangId FROM dbo.HoaDons ORDER BY HoaDonId")).ToList();
        Assert.Equal(2, invoices.Count);
        Assert.Equal(("HD001", KhachHangAId), invoices[0]);
        Assert.Equal(("HD002", KhachHangBId), invoices[1]);
    }

    [Fact]
    public async Task Test06_LuuThatBai_Rollback_KhongDeLaiHeaderDetailRac()
    {
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        var headerBefore = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.HoaDons;");
        var detailBefore = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ChiTietHoaDons;");

        // Gửi yêu cầu với sản phẩm không tồn tại (sẽ fail trong transaction do FK hoặc check giá)
        var req = new TaoHoaDonRequest
        {
            KhachHangId = KhachHangAId,
            MaHoaDon = string.Empty,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = 999999, SoLuong = 1, DonGiaHienThi = 100000 }
            }
        };

        var res = await _useCase.ExecuteAsync(req);
        Assert.False(res.ThanhCong);

        // Đảm bảo không có header hoặc detail rác nào được chèn lại
        var headerAfter = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.HoaDons;");
        var detailAfter = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ChiTietHoaDons;");

        Assert.Equal(headerBefore, headerAfter);
        Assert.Equal(detailBefore, detailAfter);
    }
}
