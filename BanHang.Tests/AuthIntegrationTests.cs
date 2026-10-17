using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.PluginInterfaces.Security;
using BanHang.UseCases.Security;
using Dapper;
using DuAn_BanHangVaCongNoKhachHang.Services;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Integration Tests cho xác thực và phân quyền trên database kiểm thử riêng: QuanLyBanHangCongNo_Test
/// Tuyệt đối KHÔNG tác động tới database chính.
/// </summary>
public class AuthIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyBanHangCongNo_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly NguoiDungRepository _nguoiDungRepo;
    private readonly IPasswordHasherService _hasher = new PasswordHasherService();
    private readonly ILoginRateLimiter _rateLimiter = new MemoryLoginRateLimiter();
    private readonly DangNhapUseCase _dangNhapUseCase;

    public AuthIntegrationTests()
    {
        _connectionFactory = new SqlConnectionFactory(TestConnectionString);
        _nguoiDungRepo = new NguoiDungRepository(_connectionFactory);
        _dangNhapUseCase = new DangNhapUseCase(_nguoiDungRepo, _hasher, _rateLimiter);
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

        // 2. Chạy script tạo bảng dbo.NguoiDungs nếu chưa tồn tại
        using (var testConn = new SqlConnection(TestConnectionString))
        {
            await testConn.OpenAsync();

            const string sqlScript = @"
                IF OBJECT_ID('dbo.NguoiDungs', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.NguoiDungs
                    (
                        NguoiDungId     INT IDENTITY(1,1)   NOT NULL,
                        TenDangNhap     NVARCHAR(50)        NOT NULL,
                        MatKhauHash     NVARCHAR(256)       NOT NULL,
                        HoTen           NVARCHAR(100)       NOT NULL,
                        VaiTro          NVARCHAR(50)        NOT NULL,
                        TrangThai       BIT                 NOT NULL CONSTRAINT DF_NguoiDungs_TrangThai DEFAULT (1),
                        NgayTao         DATETIME            NOT NULL CONSTRAINT DF_NguoiDungs_NgayTao DEFAULT (GETDATE()),

                        CONSTRAINT PK_NguoiDungs PRIMARY KEY CLUSTERED (NguoiDungId),
                        CONSTRAINT UQ_NguoiDungs_TenDangNhap UNIQUE (TenDangNhap),
                        CONSTRAINT CK_NguoiDungs_VaiTro CHECK (VaiTro IN (N'NhanVienBanHang', N'KeToanCongNo'))
                    );

                    CREATE NONCLUSTERED INDEX IX_NguoiDungs_TenDangNhap ON dbo.NguoiDungs(TenDangNhap);
                END";

            await testConn.ExecuteAsync(sqlScript);

            // Làm sạch dữ liệu bảng NguoiDungs để test độc lập
            await testConn.ExecuteAsync("DELETE FROM dbo.NguoiDungs;");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Test01_CheckConstraint_ChiChoPhepHaiVaiTro()
    {
        using var conn = new SqlConnection(TestConnectionString);
        await conn.OpenAsync();

        // 1. Thêm vai trò hợp lệ: NhanVienBanHang -> Thành công
        var validUser1 = new NguoiDung
        {
            TenDangNhap = "valid_nvbh",
            MatKhauHash = _hasher.HashPassword("Pass123"),
            HoTen = "Valid NVBH",
            VaiTro = VaiTroConstants.NhanVienBanHang,
            TrangThai = true,
            NgayTao = DateTime.Now
        };
        int id1 = await _nguoiDungRepo.TaoNguoiDungAsync(validUser1);
        Assert.True(id1 > 0);

        // 2. Thêm vai trò hợp lệ: KeToanCongNo -> Thành công
        var validUser2 = new NguoiDung
        {
            TenDangNhap = "valid_ktcn",
            MatKhauHash = _hasher.HashPassword("Pass123"),
            HoTen = "Valid KTCN",
            VaiTro = VaiTroConstants.KeToanCongNo,
            TrangThai = true,
            NgayTao = DateTime.Now
        };
        int id2 = await _nguoiDungRepo.TaoNguoiDungAsync(validUser2);
        Assert.True(id2 > 0);

        // 3. Thử thêm vai trò 'Admin' hoặc khác -> SQL Server ném SqlException do vi phạm CHECK constraint
        var invalidUser = new NguoiDung
        {
            TenDangNhap = "admin_user",
            MatKhauHash = _hasher.HashPassword("Pass123"),
            HoTen = "Admin Root",
            VaiTro = "Admin", // Không thuộc CHECK constraint
            TrangThai = true,
            NgayTao = DateTime.Now
        };

        var ex = await Assert.ThrowsAsync<SqlException>(() => _nguoiDungRepo.TaoNguoiDungAsync(invalidUser));
        Assert.Contains("CK_NguoiDungs_VaiTro", ex.Message);
    }

    [Fact]
    public async Task Test02_UniqueConstraint_TenDangNhapKhongTrungLap()
    {
        var user1 = new NguoiDung
        {
            TenDangNhap = "unique_nvbh",
            MatKhauHash = _hasher.HashPassword("Pass1"),
            HoTen = "User 1",
            VaiTro = VaiTroConstants.NhanVienBanHang,
            TrangThai = true,
            NgayTao = DateTime.Now
        };
        await _nguoiDungRepo.TaoNguoiDungAsync(user1);

        var user2 = new NguoiDung
        {
            TenDangNhap = "unique_nvbh", // Trùng tên đăng nhập
            MatKhauHash = _hasher.HashPassword("Pass2"),
            HoTen = "User 2",
            VaiTro = VaiTroConstants.KeToanCongNo,
            TrangThai = true,
            NgayTao = DateTime.Now
        };

        var ex = await Assert.ThrowsAsync<SqlException>(() => _nguoiDungRepo.TaoNguoiDungAsync(user2));
        Assert.Contains("UQ_NguoiDungs_TenDangNhap", ex.Message);
    }

    [Fact]
    public async Task Test03_Seeder_KhoiTaoDemo_IdempotentKhongGhiDe()
    {
        // 1. Lần 1: Seed hai tài khoản nvbh và ktcn
        await NguoiDungSeeder.SeedDemoAccountsAsync(_nguoiDungRepo, _hasher, "Nvbh@2026!", "Ktcn@2026!");

        var nvbh = await _nguoiDungRepo.LayTheoTenDangNhapAsync("nvbh");
        var ktcn = await _nguoiDungRepo.LayTheoTenDangNhapAsync("ktcn");

        Assert.NotNull(nvbh);
        Assert.NotNull(ktcn);
        Assert.Equal(VaiTroConstants.NhanVienBanHang, nvbh.VaiTro);
        Assert.Equal(VaiTroConstants.KeToanCongNo, ktcn.VaiTro);

        // Đảm bảo mật khẩu được băm chuẩn xác
        Assert.True(_hasher.VerifyPassword(nvbh.MatKhauHash, "Nvbh@2026!"));
        Assert.True(_hasher.VerifyPassword(ktcn.MatKhauHash, "Ktcn@2026!"));

        string originalHash = nvbh.MatKhauHash;

        // 2. Lần 2: Chạy lại seeder với mật khẩu khác -> Không ghi đè tài khoản cũ
        await NguoiDungSeeder.SeedDemoAccountsAsync(_nguoiDungRepo, _hasher, "MatKhauMoi!", "MatKhauMoi!");

        var nvbhAfter = await _nguoiDungRepo.LayTheoTenDangNhapAsync("nvbh");
        Assert.NotNull(nvbhAfter);
        Assert.Equal(originalHash, nvbhAfter.MatKhauHash); // Vẫn giữ nguyên hash ban đầu
    }

    [Fact]
    public async Task Test04_DangNhapThanhCong_VoiDuLieuDatabaseTest()
    {
        await NguoiDungSeeder.SeedDemoAccountsAsync(_nguoiDungRepo, _hasher, "DemoSales@123", "DemoAccountant@456");

        // Đăng nhập nvbh
        var res1 = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = "nvbh",
            MatKhau = "DemoSales@123"
        });
        Assert.True(res1.ThanhCong);
        Assert.Equal(VaiTroConstants.NhanVienBanHang, res1.NguoiDung?.VaiTro);

        // Đăng nhập ktcn
        var res2 = await _dangNhapUseCase.ExecuteAsync(new DangNhapRequest
        {
            TenDangNhap = "ktcn",
            MatKhau = "DemoAccountant@456"
        });
        Assert.True(res2.ThanhCong);
        Assert.Equal(VaiTroConstants.KeToanCongNo, res2.NguoiDung?.VaiTro);
    }
}
