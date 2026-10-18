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
/// Kiểm thử tích hợp quản lý sản phẩm trên DATABASE KIỂM THỬ RIÊNG: QuanLyBanHangCongNo_Test
/// Tuyệt đối không thay đổi hay ghi dữ liệu vào database chính QuanLyBanHangCongNo
/// </summary>
public class SanPhamIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly IThemSanPhamUseCase _themSanPhamUseCase;
    private readonly ICapNhatSanPhamUseCase _capNhatSanPhamUseCase;
    private readonly IDoiTrangThaiSanPhamUseCase _doiTrangThaiSanPhamUseCase;
    private readonly IXemDanhSachSanPhamUseCase _xemDanhSachSanPhamUseCase;

    private readonly IHoaDonRepository _hoaDonRepo;
    private readonly ITaoHoaDonBanChiuUseCase _taoHoaDonUseCase;

    public int KhachHangTestId { get; private set; }

    public SanPhamIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        var ktcnUser = FakeCurrentUserService.KeToanCongNo();
        var nvbhUser = FakeCurrentUserService.NhanVienBanHang();

        _sanPhamRepo = new SanPhamRepository(_connectionFactory);
        _themSanPhamUseCase = new ThemSanPhamUseCase(_sanPhamRepo, ktcnUser);
        _capNhatSanPhamUseCase = new CapNhatSanPhamUseCase(_sanPhamRepo, ktcnUser);
        _doiTrangThaiSanPhamUseCase = new DoiTrangThaiSanPhamUseCase(_sanPhamRepo, ktcnUser);
        _xemDanhSachSanPhamUseCase = new XemDanhSachSanPhamUseCase(_sanPhamRepo, ktcnUser);

        _hoaDonRepo = new HoaDonRepository(_connectionFactory);
        _taoHoaDonUseCase = new TaoHoaDonBanChiuUseCase(_hoaDonRepo, nvbhUser);
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

            // Xóa sạch dữ liệu test cũ
            await testConn.ExecuteAsync(@"
                DELETE FROM dbo.PhanBoThanhToans;
                DELETE FROM dbo.ThanhToans;
                DELETE FROM dbo.ChiTietHoaDons;
                DELETE FROM dbo.HoaDons;
                DELETE FROM dbo.SanPhams;
                DELETE FROM dbo.KhachHangs;");

            // Seed 1 khách hàng phục vụ kiểm thử lập hóa đơn
            KhachHangTestId = await testConn.ExecuteScalarAsync<int>(@"
                INSERT INTO dbo.KhachHangs (MaKhachHang, TenKhachHang, HanMucCongNo, TrangThai)
                VALUES ('KH_SP_TEST', N'Khách Hàng Kiểm Thử Sản Phẩm', 50000000, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Test01_ThemSanPham_HopLe_LuuDungSchemaVaTrimKhoangTrang()
    {
        var req = new TaoSanPhamRequest
        {
            MaSanPham = "  SP_INT_01  ",
            TenSanPham = "  Chuột Không Dây Gaming  ",
            DonViTinh = "  Cái  ",
            DonGia = 750000.50m,
            MoTa = "  Chuột cảm biến quang học  ",
            HinhAnh = "  /images/mouse.png  ",
            TrangThai = true
        };

        var res = await _themSanPhamUseCase.ExecuteAsync(req);

        Assert.True(res.ThanhCong, res.ThongBao);
        Assert.NotNull(res.SanPhamIdMoi);
        Assert.True(res.SanPhamIdMoi.Value > 0);

        int spId = res.SanPhamIdMoi.Value;

        using var conn = new SqlConnection(TestConnectionString);
        var spInDb = await conn.QuerySingleOrDefaultAsync<SanPham>(
            "SELECT * FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });

        Assert.NotNull(spInDb);
        Assert.Equal("SP_INT_01", spInDb.MaSanPham);
        Assert.Equal("Chuột Không Dây Gaming", spInDb.TenSanPham);
        Assert.Equal("Cái", spInDb.DonViTinh);
        Assert.Equal(750000.50m, spInDb.DonGia);
        Assert.Equal(0, spInDb.SoLuongTon); // Giá trị mặc định của database
        Assert.Equal("Chuột cảm biến quang học", spInDb.MoTa);
        Assert.Equal("/images/mouse.png", spInDb.HinhAnh);
        Assert.True(spInDb.TrangThai);

        var ngayTao = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT NgayTao FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        Assert.True((DateTime.Now - ngayTao).TotalMinutes < 5);
    }

    [Fact]
    public async Task Test02_TrungMaSanPham_KhongTaoThemDuLieu_BaoLoiRanhMach()
    {
        var req1 = new TaoSanPhamRequest
        {
            MaSanPham = "SP_UNIQUE_01",
            TenSanPham = "Bàn Phím Cơ Thứ Nhất",
            DonViTinh = "Cái",
            DonGia = 1200000,
            TrangThai = true
        };
        var res1 = await _themSanPhamUseCase.ExecuteAsync(req1);
        Assert.True(res1.ThanhCong);

        // Thêm sản phẩm thứ 2 trùng mã (kể cả có khoảng trắng)
        var req2 = new TaoSanPhamRequest
        {
            MaSanPham = "  SP_UNIQUE_01  ",
            TenSanPham = "Bàn Phím Cơ Thứ Hai Trùng Mã",
            DonViTinh = "Cái",
            DonGia = 1500000,
            TrangThai = true
        };
        var res2 = await _themSanPhamUseCase.ExecuteAsync(req2);

        Assert.False(res2.ThanhCong);
        Assert.True(res2.LoiTheoTruong.ContainsKey("MaSanPham"));
        Assert.Contains("đã được sử dụng", res2.LoiTheoTruong["MaSanPham"]);

        using var conn = new SqlConnection(TestConnectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.SanPhams WHERE MaSanPham = 'SP_UNIQUE_01'");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Test03_SuaSanPham_KhongLamThayDoiId_Ma_NgayTao_VaCapNhatDungTonKho()
    {
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_SUA_01",
            TenSanPham = "Tai Nghe Gaming Ban Đầu",
            DonViTinh = "Cái",
            DonGia = 600000,
            SoLuongTon = 25,
            TrangThai = true
        });
        Assert.True(addRes.ThanhCong);
        int spId = addRes.SanPhamIdMoi!.Value;

        using var conn = new SqlConnection(TestConnectionString);
        var ngayTaoGoc = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT NgayTao FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });

        // Cập nhật sản phẩm: cập nhật tên, ĐVT, giá, ảnh, mô tả và tồn kho mới = 60
        var updateReq = new CapNhatSanPhamRequest
        {
            SanPhamId = spId,
            MaSanPham = "SP_SUA_01",
            TenSanPham = "Tai Nghe Chống Ồn Cao Cấp",
            DonViTinh = "Hộp",
            DonGia = 950000,
            SoLuongTon = 60,
            MoTa = "Có Bluetooth và mic",
            HinhAnh = "/images/headphone_v2.png",
            TrangThai = true
        };
        var updateRes = await _capNhatSanPhamUseCase.ExecuteAsync(updateReq);

        Assert.True(updateRes.ThanhCong, updateRes.ThongBao);

        var spSauSua = await conn.QuerySingleAsync<SanPham>(
            "SELECT * FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        var ngayTaoSauSua = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT NgayTao FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });

        // SanPhamId, MaSanPham, NgayTao phải được giữ nguyên tuyệt đối
        Assert.Equal(spId, spSauSua.SanPhamId);
        Assert.Equal("SP_SUA_01", spSauSua.MaSanPham);
        Assert.Equal(ngayTaoGoc, ngayTaoSauSua);

        // SoLuongTon được cập nhật đúng giá trị mới
        Assert.Equal(60, spSauSua.SoLuongTon);

        // Các trường sửa đổi được cập nhật
        Assert.Equal("Tai Nghe Chống Ồn Cao Cấp", spSauSua.TenSanPham);
        Assert.Equal("Hộp", spSauSua.DonViTinh);
        Assert.Equal(950000, spSauSua.DonGia);
        Assert.Equal("Có Bluetooth và mic", spSauSua.MoTa);
        Assert.Equal("/images/headphone_v2.png", spSauSua.HinhAnh);
    }

    [Fact]
    public async Task Test04_SuaDanhMuc_KhongThayDoiSnapshot_TrongChiTietHoaDonCu()
    {
        // 1. Tạo sản phẩm giá 1,000,000 đ
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_SNAPSHOT_01",
            TenSanPham = "Màn Hình IPS 24 inch",
            DonViTinh = "Cái",
            DonGia = 1000000,
            SoLuongTon = 50,
            TrangThai = true
        });
        int spId = addRes.SanPhamIdMoi!.Value;

        // 2. Lập hóa đơn bán chịu với sản phẩm này (SL: 2, Đơn giá: 1M, Thành tiền: 2M)
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_SNAP_01",
            KhachHangId = KhachHangTestId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(15),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = spId, SoLuong = 2, DonGiaHienThi = 1000000 }
            }
        });
        Assert.True(hdRes.ThanhCong);

        // 3. Sửa thông tin sản phẩm trong danh mục: đổi tên thành "Màn Hình Oled 24 inch" và tăng giá lên 3,000,000 đ
        var updateRes = await _capNhatSanPhamUseCase.ExecuteAsync(new CapNhatSanPhamRequest
        {
            SanPhamId = spId,
            MaSanPham = "SP_SNAPSHOT_01",
            TenSanPham = "Màn Hình Oled 24 inch Đã Đổi Tên",
            DonViTinh = "Bộ",
            DonGia = 3000000,
            TrangThai = true
        });
        Assert.True(updateRes.ThanhCong);

        // 4. Kiểm tra snapshot trong ChiTietHoaDons cũ: Tên, đơn vị tính, đơn giá, thành tiền vẫn nguyên vẹn 100%
        using var conn = new SqlConnection(TestConnectionString);
        var ctInDb = await conn.QuerySingleAsync(@"
            SELECT TenSanPham, DonViTinh, SoLuong, DonGia, ThanhTien 
            FROM dbo.ChiTietHoaDons 
            WHERE SanPhamId = @SanPhamId", new { SanPhamId = spId });

        Assert.Equal("Màn Hình IPS 24 inch", ctInDb.TenSanPham);
        Assert.Equal("Cái", ctInDb.DonViTinh);
        Assert.Equal(2, (int)ctInDb.SoLuong);
        Assert.Equal(1000000, (decimal)ctInDb.DonGia);
        Assert.Equal(2000000, (decimal)ctInDb.ThanhTien);
    }

    [Fact]
    public async Task Test05_NgungBan_ChanTaoHoaDonMoi_NhungHoaDonCuConNguyen()
    {
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_NGUNGBAN_01",
            TenSanPham = "Cáp HDMI Cũ",
            DonViTinh = "Sợi",
            DonGia = 150000,
            SoLuongTon = 20,
            TrangThai = true
        });
        int spId = addRes.SanPhamIdMoi!.Value;

        // Lập 1 hóa đơn khi còn đang bán
        var hdOk = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_BEFORE_STOP",
            KhachHangId = KhachHangTestId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = spId, SoLuong = 1, DonGiaHienThi = 150000 }
            }
        });
        Assert.True(hdOk.ThanhCong);

        // Ngừng bán sản phẩm (TrangThai = false)
        var stopRes = await _doiTrangThaiSanPhamUseCase.ExecuteAsync(spId, false);
        Assert.True(stopRes.ThanhCong);

        // Kiểm tra trong DB: dòng dữ liệu không bị DELETE, TrangThai = 0
        using var conn = new SqlConnection(TestConnectionString);
        var spDb = await conn.QuerySingleOrDefaultAsync<SanPham>(
            "SELECT * FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        Assert.NotNull(spDb);
        Assert.False(spDb.TrangThai);

        // Cố tạo hóa đơn mới với sản phẩm đã ngừng bán -> Bị từ chối
        var hdFail = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_AFTER_STOP_FAIL",
            KhachHangId = KhachHangTestId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = spId, SoLuong = 1, DonGiaHienThi = 150000 }
            }
        });
        Assert.False(hdFail.ThanhCong);
        Assert.Contains("ngừng kinh doanh", hdFail.ThongBao.ToLower());

        // Hóa đơn cũ HD_BEFORE_STOP vẫn tồn tại nguyên vẹn
        var countHdCu = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.HoaDons WHERE MaHoaDon = 'HD_BEFORE_STOP'");
        Assert.Equal(1, countHdCu);
    }

    [Fact]
    public async Task Test06_MoBanLai_ChoPhepTaoHoaDonBinhThuong()
    {
        // Tạo sản phẩm ban đầu ngừng bán
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_MOLAI_01",
            TenSanPham = "Chuột Công Thái Học Mới Về",
            DonViTinh = "Cái",
            DonGia = 800000,
            SoLuongTon = 50,
            TrangThai = false // Ngừng bán
        });
        int spId = addRes.SanPhamIdMoi!.Value;

        // Mở bán lại sản phẩm
        var openRes = await _doiTrangThaiSanPhamUseCase.ExecuteAsync(spId, true);
        Assert.True(openRes.ThanhCong);

        using var conn = new SqlConnection(TestConnectionString);
        var trangThaiDb = await conn.ExecuteScalarAsync<bool>(
            "SELECT TrangThai FROM dbo.SanPhams WHERE SanPhamId = @Id", new { Id = spId });
        Assert.True(trangThaiDb);

        // Lập hóa đơn mới -> Thành công
        var hdRes = await _taoHoaDonUseCase.ExecuteAsync(new TaoHoaDonRequest
        {
            MaHoaDon = "HD_MOLAI_SUCCESS",
            KhachHangId = KhachHangTestId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = spId, SoLuong = 3, DonGiaHienThi = 800000 }
            }
        });
        Assert.True(hdRes.ThanhCong, hdRes.ThongBao);
    }

    [Fact]
    public async Task Test07_SanPhamBiNgungBanSauKhiMoForm_PhatHienKhiLuuHoaDon()
    {
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_RACE_STATUS",
            TenSanPham = "Webcam HD",
            DonViTinh = "Cái",
            DonGia = 450000,
            TrangThai = true
        });
        int spId = addRes.SanPhamIdMoi!.Value;

        // Mô phỏng người dùng đã mở form lập hóa đơn (khi đó SP đang bán)
        var hoaDonRequest = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_RACE_STATUS_01",
            KhachHangId = KhachHangTestId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = spId, SoLuong = 1, DonGiaHienThi = 450000 }
            }
        };

        // Nhưng ngay trước khi người dùng bấm Lưu, sản phẩm bị quản trị viên ngừng bán
        await _doiTrangThaiSanPhamUseCase.ExecuteAsync(spId, false);

        // Lưu hóa đơn -> Server phát hiện và từ chối lưu
        var res = await _taoHoaDonUseCase.ExecuteAsync(hoaDonRequest);
        Assert.False(res.ThanhCong);
        Assert.Contains("ngừng kinh doanh", res.ThongBao.ToLower());
    }

    [Fact]
    public async Task Test08_SanPhamBiDoiGiaSauKhiMoForm_PhatHienYeuCauXemLaiGia()
    {
        var addRes = await _themSanPhamUseCase.ExecuteAsync(new TaoSanPhamRequest
        {
            MaSanPham = "SP_RACE_PRICE",
            TenSanPham = "Loa Bluetooth Mini",
            DonViTinh = "Cái",
            DonGia = 300000,
            TrangThai = true
        });
        int spId = addRes.SanPhamIdMoi!.Value;

        // Mô phỏng người dùng mở form với giá 300,000
        var hoaDonRequest = new TaoHoaDonRequest
        {
            MaHoaDon = "HD_RACE_PRICE_01",
            KhachHangId = KhachHangTestId,
            NgayLap = DateTime.Today,
            HanThanhToan = DateTime.Today.AddDays(10),
            ChiTietHoaDons = new List<TaoChiTietHoaDonDto>
            {
                new TaoChiTietHoaDonDto { SanPhamId = spId, SoLuong = 2, DonGiaHienThi = 300000 }
            }
        };

        // Nhưng giá trong danh mục đã được cập nhật tăng lên 350,000
        await _capNhatSanPhamUseCase.ExecuteAsync(new CapNhatSanPhamRequest
        {
            SanPhamId = spId,
            MaSanPham = "SP_RACE_PRICE",
            TenSanPham = "Loa Bluetooth Mini",
            DonViTinh = "Cái",
            DonGia = 350000, // Giá mới
            TrangThai = true
        });

        // Lưu hóa đơn -> Server phát hiện giá đã đổi, từ chối và bật cờ GiaBiThayDoi
        var res = await _taoHoaDonUseCase.ExecuteAsync(hoaDonRequest);
        Assert.False(res.ThanhCong);
        Assert.True(res.GiaBiThayDoi);
        Assert.Contains("đã thay đổi", res.ThongBao.ToLower());
    }
}
