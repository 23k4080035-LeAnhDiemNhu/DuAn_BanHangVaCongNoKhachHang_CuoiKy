using System;
using System.Data;
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
/// Kiểm thử toàn diện tính năng chỉnh sửa tồn kho trong form Thêm/Sửa sản phẩm.
/// Dữ liệu kiểm thử chỉ thực thi trên QuanLyBanHangCongNo_Test.
/// </summary>
public class SanPhamTonKhoTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly IThemSanPhamUseCase _themSanPhamUseCase;
    private readonly ICapNhatSanPhamUseCase _capNhatSanPhamUseCase;
    private readonly IHoaDonRepository _hoaDonRepo;
    private readonly ITaoHoaDonBanChiuUseCase _taoHoaDonUseCase;

    public int KhachHangTestId { get; private set; }

    public SanPhamTonKhoTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        var nvbhUser = FakeCurrentUserService.NhanVienBanHang();

        _sanPhamRepo = new SanPhamRepository(_connectionFactory);
        _themSanPhamUseCase = new ThemSanPhamUseCase(_sanPhamRepo, ktcnUser);
        _capNhatSanPhamUseCase = new CapNhatSanPhamUseCase(_sanPhamRepo, ktcnUser);

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
                VALUES ('KH_TONKHO_TEST', N'Khách Hàng Test Tồn Kho', 100000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test01_ThemSanPham_TonKhoMacDinh0_LuuDungDatabase()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "SP_TON_01",
            TenSanPham = "Chuột Văn Phòng Cơ Bản",
            DonViTinh = "Cái",
            DonGia = 150000,
            SoLuongTon = 0, // Mặc định 0
            TrangThai = true
        };

        var result = await _themSanPhamUseCase.ExecuteAsync(req);
        Assert.True(result.ThanhCong, result.ThongBao);

        var sp = await _sanPhamRepo.LaySanPhamTheoIdAsync(result.SanPhamIdMoi!.Value);
        Assert.NotNull(sp);
        Assert.Equal(0, sp.SoLuongTon);
    }

    [Fact]
    public async Task Test02_ThemSanPham_TonKhoDuong_LuuDungDatabase()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "SP_TON_02",
            TenSanPham = "Bàn Phím Cơ TKL",
            DonViTinh = "Cái",
            DonGia = 850000,
            SoLuongTon = 125, // Tồn dương
            TrangThai = true
        };

        var result = await _themSanPhamUseCase.ExecuteAsync(req);
        Assert.True(result.ThanhCong, result.ThongBao);

        var sp = await _sanPhamRepo.LaySanPhamTheoIdAsync(result.SanPhamIdMoi!.Value);
        Assert.NotNull(sp);
        Assert.Equal(125, sp.SoLuongTon);
    }

    [Fact]
    public async Task Test03_CapNhatSanPham_SuaTonKhoVaDocLai_DungGiaTriMoi()
    {
        // 1. Thêm ban đầu với tồn = 50
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_TON_03",
            TenSanPham = "Tai Nghe Studio",
            DonViTinh = "Cái",
            DonGia = 1200000,
            SoLuongTon = 50,
            TrangThai = true
        });
        Assert.True(addRes.ThanhCong);
        int spId = addRes.SanPhamIdMoi!.Value;

        // 2. Cập nhật tồn kho thành 180
        var updateRes = await _capNhatSanPhamUseCase.ExecuteAsync(new CapNhatSanPhamRequest
        {
            SanPhamId = spId,
            MaSanPham = "SP_TON_03",
            TenSanPham = "Tai Nghe Studio Bản V2",
            DonViTinh = "Cái",
            DonGia = 1350000,
            SoLuongTon = 180,
            TrangThai = true
        });
        Assert.True(updateRes.ThanhCong, updateRes.ThongBao);

        // 3. Đọc lại từ repository và đối chiếu
        var spSauSua = await _sanPhamRepo.LaySanPhamTheoIdAsync(spId);
        Assert.NotNull(spSauSua);
        Assert.Equal(180, spSauSua.SoLuongTon);
        Assert.Equal("Tai Nghe Studio Bản V2", spSauSua.TenSanPham);
        Assert.Equal(1350000, spSauSua.DonGia);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Test04_Validation_TuChoiSoLuongTonAm(decimal tonAm)
    {
        // 1. Kiểm tra khi Thêm
        var themReq = new TaoSanPhamRequest
        {
            MaSanPham = "SP_TON_AM",
            TenSanPham = "Sản phẩm Tồn Âm",
            DonViTinh = "Cái",
            DonGia = 100000,
            SoLuongTon = tonAm
        };
        var themRes = await _themSanPhamUseCase.ExecuteAsync(themReq);
        Assert.False(themRes.ThanhCong);
        Assert.True(themRes.LoiTheoTruong.ContainsKey("SoLuongTon"));
        Assert.Contains("không được âm", themRes.LoiTheoTruong["SoLuongTon"]);

        // 2. Kiểm tra khi Sửa
        var suaReq = new CapNhatSanPhamRequest
        {
            SanPhamId = 1,
            MaSanPham = "SP_TON_AM",
            TenSanPham = "Sản phẩm Tồn Âm",
            DonViTinh = "Cái",
            DonGia = 100000,
            SoLuongTon = tonAm
        };
        var suaRes = await _capNhatSanPhamUseCase.ExecuteAsync(suaReq);
        Assert.False(suaRes.ThanhCong);
        Assert.True(suaRes.LoiTheoTruong.ContainsKey("SoLuongTon"));
        Assert.Contains("không được âm", suaRes.LoiTheoTruong["SoLuongTon"]);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(10.25)]
    [InlineData(0.01)]
    public async Task Test05_Validation_TuChoiSoLuongTonSoLe(decimal tonSoLe)
    {
        // 1. Kiểm tra khi Thêm
        var themReq = new TaoSanPhamRequest
        {
            MaSanPham = "SP_TON_LE",
            TenSanPham = "Sản phẩm Tồn Lẻ",
            DonViTinh = "Cái",
            DonGia = 100000,
            SoLuongTon = tonSoLe
        };
        var themRes = await _themSanPhamUseCase.ExecuteAsync(themReq);
        Assert.False(themRes.ThanhCong);
        Assert.True(themRes.LoiTheoTruong.ContainsKey("SoLuongTon"));
        Assert.Equal("Số lượng tồn kho phải là số nguyên.", themRes.LoiTheoTruong["SoLuongTon"]);

        // 2. Kiểm tra khi Sửa
        var suaReq = new CapNhatSanPhamRequest
        {
            SanPhamId = 1,
            MaSanPham = "SP_TON_LE",
            TenSanPham = "Sản phẩm Tồn Lẻ",
            DonViTinh = "Cái",
            DonGia = 100000,
            SoLuongTon = tonSoLe
        };
        var suaRes = await _capNhatSanPhamUseCase.ExecuteAsync(suaReq);
        Assert.False(suaRes.ThanhCong);
        Assert.True(suaRes.LoiTheoTruong.ContainsKey("SoLuongTon"));
        Assert.Equal("Số lượng tồn kho phải là số nguyên.", suaRes.LoiTheoTruong["SoLuongTon"]);
    }

    [Fact]
    public async Task Test06_Validation_TuChoiSoLuongTonVuotGioiHanInt()
    {
        decimal tonVuot = (decimal)int.MaxValue + 1; // 2,147,483,648

        // 1. Kiểm tra khi Thêm
        var themReq = new TaoSanPhamRequest
        {
            MaSanPham = "SP_TON_MAX",
            TenSanPham = "Sản phẩm Vượt Giới Hạn INT",
            DonViTinh = "Cái",
            DonGia = 100000,
            SoLuongTon = tonVuot
        };
        var themRes = await _themSanPhamUseCase.ExecuteAsync(themReq);
        Assert.False(themRes.ThanhCong);
        Assert.True(themRes.LoiTheoTruong.ContainsKey("SoLuongTon"));
        Assert.Contains("vượt quá giới hạn cho phép", themRes.LoiTheoTruong["SoLuongTon"]);

        // 2. Kiểm tra khi Sửa
        var suaReq = new CapNhatSanPhamRequest
        {
            SanPhamId = 1,
            MaSanPham = "SP_TON_MAX",
            TenSanPham = "Sản phẩm Vượt Giới Hạn INT",
            DonViTinh = "Cái",
            DonGia = 100000,
            SoLuongTon = tonVuot
        };
        var suaRes = await _capNhatSanPhamUseCase.ExecuteAsync(suaReq);
        Assert.False(suaRes.ThanhCong);
        Assert.True(suaRes.LoiTheoTruong.ContainsKey("SoLuongTon"));
        Assert.Contains("vượt quá giới hạn cho phép", suaRes.LoiTheoTruong["SoLuongTon"]);
    }

    [Fact]
    public async Task Test07_PhanQuyen_ChiKeToanCongNoDuocThaoTac_NhanVienBanHangBiTuChoi()
    {
        var nvbhUser = FakeCurrentUserService.NhanVienBanHang();
        var themUseCaseNV = new ThemSanPhamUseCase(_sanPhamRepo, nvbhUser);
        var capNhatUseCaseNV = new CapNhatSanPhamUseCase(_sanPhamRepo, nvbhUser);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            themUseCaseNV.ExecuteAsync(new TaoSanPhamRequest
            {
                MaSanPham = "SP_UNAUTH_01",
                TenSanPham = "Không Được Phép Thêm",
                DonViTinh = "Cái",
                DonGia = 100000,
                SoLuongTon = 10
            }));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            capNhatUseCaseNV.ExecuteAsync(new CapNhatSanPhamRequest
            {
                SanPhamId = 1,
                MaSanPham = "SP_UNAUTH_01",
                TenSanPham = "Không Được Phép Sửa",
                DonViTinh = "Cái",
                DonGia = 100000,
                SoLuongTon = 20
            }));
    }

    [Fact]
    public async Task Test08_SuaTonKho_BaoToanTruongKhacVaSnapshotHoaDonCu()
    {
        // 1. Tạo sản phẩm với tồn 100
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_SNAP_TON",
            TenSanPham = "Màn Hình Cong 34 inch",
            DonViTinh = "Cái",
            DonGia = 8000000,
            SoLuongTon = 100,
            TrangThai = true
        });
        int spId = addRes.SanPhamIdMoi!.Value;

        // 2. Lập hóa đơn cũ với sản phẩm này (SL: 3, Đơn giá snapshot: 8,000,000 đ)
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_TON_SNAP_01",
            KhachHangId = KhachHangTestId,
            HanThanhToan = DateTime.Today.AddDays(30),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = spId, SoLuong = 3, DonGiaHienThi = 8000000 }
            }
        });
        Assert.True(hdRes.ThanhCong, hdRes.ThongBao);
        int hdId = hdRes.HoaDonIdMoi!.Value;

        // 3. Sửa tồn kho sản phẩm trong danh mục từ 100 thành 450
        var updateRes = await _capNhatSanPhamUseCase.ExecuteAsync(new CapNhatSanPhamRequest
        {
            SanPhamId = spId,
            MaSanPham = "SP_SNAP_TON",
            TenSanPham = "Màn Hình Cong 34 inch (Đã Cập Nhật Tên)",
            DonViTinh = "Cái",
            DonGia = 8500000, // Đơn giá mới
            SoLuongTon = 450, // Tồn kho mới
            TrangThai = true
        });
        Assert.True(updateRes.ThanhCong, updateRes.ThongBao);

        // 4. Kiểm tra snapshot chi tiết hóa đơn cũ KHÔNG bị thay đổi
        using var conn = new SqlConnection(TestConnectionString);
        var ctHienTai = await conn.QuerySingleAsync<ChiTietHoaDon>(
            "SELECT * FROM dbo.ChiTietHoaDons WHERE HoaDonId = @HdId AND SanPhamId = @SpId",
            new { HdId = hdId, SpId = spId });

        Assert.Equal(3, ctHienTai.SoLuong);
        Assert.Equal(8000000, ctHienTai.DonGia); // Vẫn là 8,000,000 đ (snapshot cũ)
        Assert.Equal(24000000, ctHienTai.ThanhTien);

        // 5. Kiểm tra tồn kho của sản phẩm trong danh mục đã cập nhật thành 450
        var spSauSua = await _sanPhamRepo.LaySanPhamTheoIdAsync(spId);
        Assert.NotNull(spSauSua);
        Assert.Equal(450, spSauSua.SoLuongTon);
        Assert.Equal("SP_SNAP_TON", spSauSua.MaSanPham); // Mã bảo toàn
    }
}
