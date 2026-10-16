using System.Data;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai ISanPhamRepository truy xuất dữ liệu sản phẩm từ SQL Server bằng Dapper
/// </summary>
public class SanPhamRepository : ISanPhamRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SanPhamRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<SanPham>> LayDanhSachSanPhamHoatDongAsync()
    {
        const string sql = @"
            SELECT 
                SanPhamId,
                MaSanPham,
                TenSanPham,
                DonViTinh,
                DonGia,
                SoLuongTon,
                MoTa,
                HinhAnh
            FROM dbo.SanPhams
            WHERE TrangThai = 1
            ORDER BY TenSanPham ASC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SanPham>(sql);
    }
}
