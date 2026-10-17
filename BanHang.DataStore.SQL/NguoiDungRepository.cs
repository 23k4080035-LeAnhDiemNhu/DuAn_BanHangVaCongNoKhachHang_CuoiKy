using System;
using System.Data;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai INguoiDungRepository truy xuất bảng dbo.NguoiDungs bằng Dapper
/// </summary>
public class NguoiDungRepository : INguoiDungRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public NguoiDungRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<NguoiDung?> LayTheoTenDangNhapAsync(string tenDangNhap)
    {
        string normalized = tenDangNhap?.Trim().ToLowerInvariant() ?? string.Empty;
        const string sql = @"
            SELECT 
                NguoiDungId,
                TenDangNhap,
                MatKhauHash,
                HoTen,
                VaiTro,
                TrangThai,
                NgayTao
            FROM dbo.NguoiDungs
            WHERE TenDangNhap = @TenDangNhap;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<NguoiDung>(sql, new { TenDangNhap = normalized });
    }

    public async Task<bool> TonTaiTenDangNhapAsync(string tenDangNhap)
    {
        string normalized = tenDangNhap?.Trim().ToLowerInvariant() ?? string.Empty;
        const string sql = @"
            SELECT COUNT(1) 
            FROM dbo.NguoiDungs 
            WHERE TenDangNhap = @TenDangNhap;";

        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TenDangNhap = normalized });
        return count > 0;
    }

    public async Task<int> TaoNguoiDungAsync(NguoiDung nguoiDung)
    {
        if (nguoiDung == null) throw new ArgumentNullException(nameof(nguoiDung));
        nguoiDung.TenDangNhap = nguoiDung.TenDangNhap?.Trim().ToLowerInvariant() ?? string.Empty;

        const string sql = @"
            INSERT INTO dbo.NguoiDungs (
                TenDangNhap,
                MatKhauHash,
                HoTen,
                VaiTro,
                TrangThai,
                NgayTao
            )
            VALUES (
                @TenDangNhap,
                @MatKhauHash,
                @HoTen,
                @VaiTro,
                @TrangThai,
                @NgayTao
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, nguoiDung);
    }

    public async Task<int> DemSoLuongNguoiDungAsync()
    {
        const string sql = @"SELECT COUNT(1) FROM dbo.NguoiDungs;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}
