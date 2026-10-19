using System.Data;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.HoaDonScreen;
using BanHang.UseCases.KhachHangScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.ThanhToanScreen;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp thêm, sửa, tạm khóa/mở lại khách hàng trên DATABASE KIỂM THỬ RIÊNG: QuanLyBanHangCongNo_Test
/// Tuyệt đối không thay đổi hay ghi dữ liệu vào database chính QuanLyBanHangCongNo
/// </summary>
public class KhachHangIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IKhachHangRepository _khachHangRepo;
    private readonly IThemKhachHangUseCase _themKhachHangUseCase;
    private readonly ICapNhatKhachHangUseCase _capNhatKhachHangUseCase;
    private readonly IDoiTrangThaiKhachHangUseCase _doiTrangThaiKhachHangUseCase;
    private readonly IXemDanhSachKhachHangUseCase _xemDanhSachKhachHangUseCase;

    private readonly IHoaDonRepository _hoaDonRepo;
    private readonly ITaoHoaDonBanChiuUseCase _taoHoaDonUseCase;
    private readonly IThanhToanRepository _thanhToanRepo;
    private readonly ITaoThanhToanUseCase _taoThanhToanUseCase;

    public int SanPhamTestId { get; private set; }

    public KhachHangIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        var nvbhUser = FakeCurrentUserService.NhanVienBanHang();

        _khachHangRepo = new KhachHangRepository(_connectionFactory);
        _themKhachHangUseCase = new ThemKhachHangUseCase(_khachHangRepo, ktcnUser);
        _capNhatKhachHangUseCase = new CapNhatKhachHangUseCase(_khachHangRepo, ktcnUser);
        _doiTrangThaiKhachHangUseCase = new DoiTrangThaiKhachHangUseCase(_khachHangRepo, ktcnUser);
        _xemDanhSachKhachHangUseCase = new XemDanhSachKhachHangUseCase(_khachHangRepo, ktcnUser);

        _hoaDonRepo = new HoaDonRepository(_connectionFactory);
        _taoHoaDonUseCase = new TaoHoaDonBanChiuUseCase(_hoaDonRepo, nvbhUser);

        _thanhToanRepo = new ThanhToanRepository(_connectionFactory);
        _taoThanhToanUseCase = new TaoThanhToanUseCase(_thanhToanRepo, ktcnUser);
    }

    public async Task InitializeAsync()
    {
        // 1. Đảm bảo database test tồn tại
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
                        SoNgayNo INT NOT NULL DEFAULT (0),
                        NgayTao DATETIME NOT NULL DEFAULT (GETDATE()),
                        TrangThai BIT NOT NULL DEFAULT (1)
                    );
                END
                ELSE
                BEGIN
                    IF COL_LENGTH('dbo.KhachHangs', 'SoNgayNo') IS NULL
                    BEGIN
                        ALTER TABLE dbo.KhachHangs ADD SoNgayNo INT NOT NULL CONSTRAINT DF_KhachHangs_SoNgayNo DEFAULT (0) CONSTRAINT CK_KhachHangs_SoNgayNo_NonNegative CHECK (SoNgayNo >= 0);
                    END
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

            // Làm sạch dữ liệu trong database test trước mỗi lần chạy test
            await testConn.ExecuteAsync(@"
                DELETE FROM dbo.PhanBoThanhToans;
                DELETE FROM dbo.ThanhToans;
                DELETE FROM dbo.ChiTietHoaDons;
                DELETE FROM dbo.HoaDons;
                DELETE FROM dbo.SanPhams;
                DELETE FROM dbo.KhachHangs;");

            // Seed 1 sản phẩm test
            SanPhamTestId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.SanPhams (MaSanPham, TenSanPham, DonViTinh, DonGia, SoLuongTon, TrangThai)
                VALUES ('SP_KH_TEST', N'Sản phẩm mẫu kiểm thử', N'Cái', 1000000, 100, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Test01_ThemKhachHang_HopLe_LuuDungSchemaVaTrimKhoangTrang()
    {
        // Act: Thêm khách hàng với khoảng trắng ở mã và tên
        var request = new TaoKhachHangRequest
        {
            MaKhachHang = "  KH_TEST_01  ",
            TenKhachHang = "  Công ty Đại Nam Test  ",
            SoDienThoai = "  0909123456  ",
            Email = "  dainam@example.com  ",
            DiaChi = "  123 Đường Lê Lợi, Q1  ",
            HanMucCongNo = 15000000.50m,
            TrangThai = true
        };

        var result = await _themKhachHangUseCase.ExecuteAsync(request);

        // Assert
        Assert.True(result.ThanhCong, result.ThongBao);
        Assert.NotNull(result.KhachHangIdMoi);
        Assert.True(result.KhachHangIdMoi.Value > 0);

        int khId = result.KhachHangIdMoi.Value;

        // Đọc lại từ database kiểm tra các trường đã được trim và đúng schema
        using var conn = new SqlConnection(TestConnectionString);
        var khInDb = await conn.QuerySingleOrDefaultAsync<KhachHang>(
            "SELECT * FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });

        Assert.NotNull(khInDb);
        Assert.Equal("KH_TEST_01", khInDb.MaKhachHang);
        Assert.Equal("Công ty Đại Nam Test", khInDb.TenKhachHang);
        Assert.Equal("0909123456", khInDb.SoDienThoai);
        Assert.Equal("dainam@example.com", khInDb.Email);
        Assert.Equal("123 Đường Lê Lợi, Q1", khInDb.DiaChi);
        Assert.Equal(15000000.50m, khInDb.HanMucCongNo);
        Assert.True(khInDb.TrangThai);

        var ngayTao = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT NgayTao FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });
        Assert.True((DateTime.Now - ngayTao).TotalMinutes < 5);

        // Kiểm tra qua XemDanhSachKhachHangUseCase: Dư nợ ban đầu phải = 0
        var danhSach = await _xemDanhSachKhachHangUseCase.ExecuteAsync();
        var khDto = danhSach.FirstOrDefault(k => k.KhachHangId == khId);
        Assert.NotNull(khDto);
        Assert.Equal(0, khDto.SoDuCongNo);
        Assert.Equal(15000000.50m, khDto.HanMucCongNo);
        Assert.False(khDto.DaVuotHanMuc);
        Assert.True(khDto.TrangThai);
    }

    [Fact]
    public async Task Test02_TrungMaKhachHang_KhongTaoThemDuLieu_BaoLoiRanhMach()
    {
        // Arrange: Thêm KH đầu tiên
        var req1 = new TaoKhachHangRequest
        {
            MaKhachHang = "KH_UNIQUE_01",
            TenKhachHang = "Khách Hàng Thứ Nhất",
            HanMucCongNo = 5000000,
            TrangThai = true
        };
        var res1 = await _themKhachHangUseCase.ExecuteAsync(req1);
        Assert.True(res1.ThanhCong);

        // Act: Thêm KH thứ hai có trùng mã KH_UNIQUE_01 (kể cả có khoảng trắng)
        var req2 = new TaoKhachHangRequest
        {
            MaKhachHang = "  KH_UNIQUE_01  ",
            TenKhachHang = "Khách Hàng Thứ Hai Trùng Mã",
            HanMucCongNo = 3000000,
            TrangThai = true
        };
        var res2 = await _themKhachHangUseCase.ExecuteAsync(req2);

        // Assert
        Assert.False(res2.ThanhCong);
        Assert.True(res2.LoiTheoTruong.ContainsKey("MaKhachHang"));
        Assert.Contains("đã tồn tại", res2.LoiTheoTruong["MaKhachHang"]);

        // Kiểm tra trong DB chỉ tồn tại đúng 1 dòng
        using var conn = new SqlConnection(TestConnectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.KhachHangs WHERE MaKhachHang = 'KH_UNIQUE_01'");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Test03_SuaKhachHang_KhongLamThayDoiId_NgayTao_HoacCongNo()
    {
        // Arrange: Tạo KH và 1 hóa đơn nợ 4,000,000
        var addRes = await _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            MaKhachHang = "KH_SUA_01",
            TenKhachHang = "Tên Ban Đầu",
            HanMucCongNo = 10000000,
            TrangThai = true
        });
        Assert.True(addRes.ThanhCong);
        int khId = addRes.KhachHangIdMoi!.Value;

        // Tạo hóa đơn bán chịu 4M
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_SUA_01",
            KhachHangId = khId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 4, DonGiaHienThi = 1000000 }
            }
        });
        Assert.True(hdRes.ThanhCong);

        // Lấy ngày tạo gốc trước khi sửa
        using var conn = new SqlConnection(TestConnectionString);
        var ngayTaoGoc = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT NgayTao FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });

        // Act: Sửa thông tin khách hàng (đổi tên, email, sdt, địa chỉ, hạn mức thành 12M)
        var updateReq = new CapNhatKhachHangRequest
        {
            KhachHangId = khId,
            MaKhachHang = "KH_SUA_01",
            TenKhachHang = "Tên Sau Khi Sửa Mới",
            SoDienThoai = "0988776655",
            Email = "suamoi@example.com",
            DiaChi = "456 Đường CMT8, Q3",
            HanMucCongNo = 12000000,
            TrangThai = true
        };
        var updateRes = await _capNhatKhachHangUseCase.ExecuteAsync(updateReq);

        // Assert
        Assert.True(updateRes.ThanhCong, updateRes.ThongBao);

        var khSauSua = await conn.QuerySingleAsync<KhachHang>(
            "SELECT * FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });
        var ngayTaoSauSua = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT NgayTao FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });

        // KhachHangId và NgayTao phải giữ nguyên tuyệt đối
        Assert.Equal(khId, khSauSua.KhachHangId);
        Assert.Equal(ngayTaoGoc, ngayTaoSauSua);
        Assert.Equal("Tên Sau Khi Sửa Mới", khSauSua.TenKhachHang);
        Assert.Equal("0988776655", khSauSua.SoDienThoai);
        Assert.Equal("suamoi@example.com", khSauSua.Email);
        Assert.Equal("456 Đường CMT8, Q3", khSauSua.DiaChi);
        Assert.Equal(12000000, khSauSua.HanMucCongNo);

        // Công nợ tính từ hóa đơn vẫn là 4M không thay đổi
        var congNo = await _khachHangRepo.LayCongNoHienTaiCuaKhachHangAsync(khId);
        Assert.Equal(4000000, congNo);

        var danhSach = await _xemDanhSachKhachHangUseCase.ExecuteAsync();
        var khDto = danhSach.First(k => k.KhachHangId == khId);
        Assert.Equal(4000000, khDto.SoDuCongNo);
        Assert.Equal(12000000, khDto.HanMucCongNo);
        Assert.False(khDto.DaVuotHanMuc);
    }

    [Fact]
    public async Task Test04_HaHanMucDuoiNo_YeuCauXacNhan_VaApDungKhiCoXacNhan()
    {
        // Arrange: KH có hạn mức 10M, tạo nợ 6M
        var addRes = await _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            MaKhachHang = "KH_HA_HANMUC_01",
            TenKhachHang = "Khách Hàng Thử Hạ Hạn Mức",
            HanMucCongNo = 10000000,
            TrangThai = true
        });
        int khId = addRes.KhachHangIdMoi!.Value;

        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_HA_01",
            KhachHangId = khId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 6, DonGiaHienThi = 1000000 } // 6M
            }
        });
        Assert.True(hdRes.ThanhCong);

        // Act 1: Hạ hạn mức xuống 4M (< nợ 6M) mà CHƯA có xác nhận (XacNhanHaHanMucDuoiNo = false)
        var reqChuaXacNhan = new CapNhatKhachHangRequest
        {
            KhachHangId = khId,
            MaKhachHang = "KH_HA_HANMUC_01",
            TenKhachHang = "Khách Hàng Thử Hạ Hạn Mức",
            HanMucCongNo = 4000000, // < 6M nợ
            TrangThai = true,
            XacNhanHaHanMucDuoiNo = false
        };
        var resChuaXacNhan = await _capNhatKhachHangUseCase.ExecuteAsync(reqChuaXacNhan);

        // Assert 1: Bị từ chối và yêu cầu xác nhận rõ ràng
        Assert.False(resChuaXacNhan.ThanhCong);
        Assert.True(resChuaXacNhan.CanXacNhanHaHanMuc);
        Assert.Contains("thấp hơn số dư công nợ hiện tại", resChuaXacNhan.ThongBao);

        // Kiểm tra trong DB hạn mức vẫn giữ nguyên 10M
        using var conn = new SqlConnection(TestConnectionString);
        var hanMucDb = await conn.ExecuteScalarAsync<decimal>(
            "SELECT HanMucCongNo FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });
        Assert.Equal(10000000, hanMucDb);

        // Act 2: Gửi lại với xác nhận XacNhanHaHanMucDuoiNo = true
        var reqCoXacNhan = new CapNhatKhachHangRequest
        {
            KhachHangId = khId,
            MaKhachHang = "KH_HA_HANMUC_01",
            TenKhachHang = "Khách Hàng Thử Hạ Hạn Mức",
            HanMucCongNo = 4000000,
            TrangThai = true,
            XacNhanHaHanMucDuoiNo = true // Đã xác nhận rõ
        };
        var resCoXacNhan = await _capNhatKhachHangUseCase.ExecuteAsync(reqCoXacNhan);

        // Assert 2: Cập nhật thành công, hạn mức mới là 4M, hóa đơn cũ 6M vẫn nguyên vẹn
        Assert.True(resCoXacNhan.ThanhCong, resCoXacNhan.ThongBao);

        var hanMucMoiDb = await conn.ExecuteScalarAsync<decimal>(
            "SELECT HanMucCongNo FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });
        Assert.Equal(4000000, hanMucMoiDb);

        // Dư nợ vẫn là 6M, và trạng thái nợ của khách hàng giờ đã là DaVuotHanMuc
        var danhSach = await _xemDanhSachKhachHangUseCase.ExecuteAsync();
        var khDto = danhSach.First(k => k.KhachHangId == khId);
        Assert.Equal(6000000, khDto.SoDuCongNo);
        Assert.Equal(4000000, khDto.HanMucCongNo);
        Assert.True(khDto.DaVuotHanMuc);

        // Act 3: Cố gắng mua thêm hóa đơn mới 1M -> Phải bị chặn vì đã vượt hạn mức mới
        var hdVuotRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_HA_02_FAIL",
            KhachHangId = khId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 1, DonGiaHienThi = 1000000 }
            }
        });
        Assert.False(hdVuotRes.ThanhCong);
        Assert.Contains("vượt quá hạn mức", hdVuotRes.ThongBao.ToLower());
    }

    [Fact]
    public async Task Test05_TamKhoa_ChanTaoHoaDonBanChiu_NhungVanChoThuTien()
    {
        // Arrange: Tạo KH và 1 hóa đơn 3M
        var addRes = await _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            MaKhachHang = "KH_KHOA_01",
            TenKhachHang = "Khách Hàng Sắp Bị Khóa",
            HanMucCongNo = 10000000,
            TrangThai = true
        });
        int khId = addRes.KhachHangIdMoi!.Value;

        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_KHOA_01",
            KhachHangId = khId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 3, DonGiaHienThi = 1000000 } // 3M
            }
        });
        Assert.True(hdRes.ThanhCong);

        // Act 1: Tạm khóa khách hàng (TrangThai = false)
        var lockRes = await _doiTrangThaiKhachHangUseCase.ExecuteAsync(khId, false);
        Assert.True(lockRes.ThanhCong, lockRes.ThongBao);

        // Kiểm tra trong DB: Khách hàng vẫn còn nguyên dòng, TrangThai = 0 (không bị DELETE)
        using var conn = new SqlConnection(TestConnectionString);
        var khInDb = await conn.QuerySingleOrDefaultAsync<KhachHang>(
            "SELECT * FROM dbo.KhachHangs WHERE KhachHangId = @Id", new { Id = khId });
        Assert.NotNull(khInDb);
        Assert.False(khInDb.TrangThai);

        // Khách hàng vẫn hiển thị trên danh sách
        var danhSach = await _xemDanhSachKhachHangUseCase.ExecuteAsync();
        var khDto = danhSach.FirstOrDefault(k => k.KhachHangId == khId);
        Assert.NotNull(khDto);
        Assert.False(khDto.TrangThai); // Trạng thái hoạt động riêng: Tạm khóa
        Assert.Equal(3000000, khDto.SoDuCongNo); // Trạng thái nợ riêng: 3M

        // Act 2: Thử tạo hóa đơn bán chịu mới cho khách hàng đang bị khóa
        var hdMoiRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_KHOA_02_BLOCKED",
            KhachHangId = khId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 1, DonGiaHienThi = 1000000 }
            }
        });

        // Assert 2: Phải bị chặn cả ở UseCase/Server
        Assert.False(hdMoiRes.ThanhCong);
        Assert.Contains("tạm khóa", hdMoiRes.ThongBao.ToLower());

        // Act 3: Khách hàng tạm khóa nộp tiền trả nợ 2M -> Phải được chấp nhận
        var ttRes = await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_KHOA_01",
            KhachHangId = khId,
            NgayThanhToan = DateTime.Today,
            SoTien = 2000000,
            PhuongThuc = "TienMat",
            GhiChu = "Khách nộp tiền dù đang tạm khóa"
        });

        // Assert 3: Thu tiền thành công, nợ giảm còn 1M
        Assert.True(ttRes.ThanhCong, ttRes.ThongBao);

        var congNoConLai = await _khachHangRepo.LayCongNoHienTaiCuaKhachHangAsync(khId);
        Assert.Equal(1000000, congNoConLai);
    }

    [Fact]
    public async Task Test06_MoLai_ChoPhepBanChiuTrongHanMuc()
    {
        // Arrange: Tạo KH tạm khóa
        var addRes = await _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            MaKhachHang = "KH_MOLAI_01",
            TenKhachHang = "Khách Hàng Mở Lại Test",
            HanMucCongNo = 10000000,
            TrangThai = false // Khởi tạo tạm khóa
        });
        int khId = addRes.KhachHangIdMoi!.Value;

        // Bị chặn tạo hóa đơn khi đang khóa
        var hdFailRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_FAIL_01",
            KhachHangId = khId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 2, DonGiaHienThi = 1000000 }
            }
        });
        Assert.False(hdFailRes.ThanhCong);

        // Act: Mở lại khách hàng (TrangThai = true)
        var unlockRes = await _doiTrangThaiKhachHangUseCase.ExecuteAsync(khId, true);
        Assert.True(unlockRes.ThanhCong, unlockRes.ThongBao);

        // Tạo hóa đơn trong hạn mức 10M -> Thành công
        var hdOkRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_OK_SAU_MOLAI",
            KhachHangId = khId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 5, DonGiaHienThi = 1000000 } // 5M <= 10M
            }
        });
        Assert.True(hdOkRes.ThanhCong, hdOkRes.ThongBao);

        var congNo = await _khachHangRepo.LayCongNoHienTaiCuaKhachHangAsync(khId);
        Assert.Equal(5000000, congNo);
    }

    [Fact]
    public async Task Test07_LichSuHoaDonVaThanhToan_ConNguyenSauKhiCapNhatKhachHang()
    {
        // Arrange: Tạo KH, 2 hóa đơn và 1 thanh toán
        var addRes = await _themKhachHangUseCase.ExecuteAsync(new TaoKhachHangRequest
        {
            MaKhachHang = "KH_LICHSU_01",
            TenKhachHang = "Khách Hàng Lưu Lịch Sử",
            HanMucCongNo = 20000000,
            TrangThai = true
        });
        int khId = addRes.KhachHangIdMoi!.Value;

        await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_LS_01",
            KhachHangId = khId,
            NgayLap = DateTime.Today.AddDays(-10),
            HanThanhToan = DateTime.Today.AddDays(5),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 3, DonGiaHienThi = 1000000 } // 3M
            }
        });

        await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_LS_02",
            KhachHangId = khId,
            NgayLap = DateTime.Today.AddDays(-5),
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = SanPhamTestId, SoLuong = 4, DonGiaHienThi = 1000000 } // 4M
            }
        });

        await _taoThanhToanUseCase.ExecuteAsync(new TaoThanhToanRequest
        {
            MaPhieuThu = "PT_LS_01",
            KhachHangId = khId,
            NgayThanhToan = DateTime.Today.AddDays(-2),
            SoTien = 3000000, // Trả hết HD1
            PhuongThuc = "ChuyenKhoan",
            GhiChu = "Trả tiền đợt 1"
        });

        // Act: Thực hiện sửa thông tin khách hàng nhiều lần và khóa rồi mở lại
        await _capNhatKhachHangUseCase.ExecuteAsync(new CapNhatKhachHangRequest
        {
            KhachHangId = khId,
            MaKhachHang = "KH_LICHSU_01",
            TenKhachHang = "Khách Hàng Lưu Lịch Sử Đã Đổi Tên",
            SoDienThoai = "0911223344",
            HanMucCongNo = 25000000,
            TrangThai = true
        });

        await _doiTrangThaiKhachHangUseCase.ExecuteAsync(khId, false); // Tạm khóa
        await _doiTrangThaiKhachHangUseCase.ExecuteAsync(khId, true);  // Mở lại

        // Assert: Kiểm tra lịch sử hóa đơn và thanh toán còn nguyên vẹn 100%
        using var conn = new SqlConnection(TestConnectionString);
        var soLuongHoaDon = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.HoaDons WHERE KhachHangId = @KhachHangId", new { KhachHangId = khId });
        var soLuongChiTiet = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM dbo.ChiTietHoaDons c
            INNER JOIN dbo.HoaDons h ON c.HoaDonId = h.HoaDonId
            WHERE h.KhachHangId = @KhachHangId", new { KhachHangId = khId });
        var soLuongThanhToan = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.ThanhToans WHERE KhachHangId = @KhachHangId", new { KhachHangId = khId });
        var soLuongPhanBo = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM dbo.PhanBoThanhToans p
            INNER JOIN dbo.ThanhToans t ON p.ThanhToanId = t.ThanhToanId
            WHERE t.KhachHangId = @KhachHangId", new { KhachHangId = khId });

        Assert.Equal(2, soLuongHoaDon);
        Assert.Equal(2, soLuongChiTiet);
        Assert.Equal(1, soLuongThanhToan);
        Assert.Equal(1, soLuongPhanBo);

        // Dư nợ còn lại: Tổng nợ 7M - đã trả 3M = 4M
        var congNo = await _khachHangRepo.LayCongNoHienTaiCuaKhachHangAsync(khId);
        Assert.Equal(4000000, congNo);
    }
}
