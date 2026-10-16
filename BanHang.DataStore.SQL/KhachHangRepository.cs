using System.Data;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai IKhachHangRepository truy xuất dữ liệu từ SQL Server bằng Dapper
/// </summary>
public class KhachHangRepository : IKhachHangRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public KhachHangRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<KhachHang>> LayDanhSachKhachHangAsync()
    {
        // Truy vấn danh sách khách hàng, tổng hợp trước ConLai của hóa đơn để tránh nhân đôi khi JOIN
        const string sql = @"
            SELECT 
                k.KhachHangId,
                k.MaKhachHang,
                k.TenKhachHang,
                k.SoDienThoai,
                k.Email,
                k.DiaChi,
                k.HanMucCongNo,
                k.TrangThai,
                ISNULL(hd.TongConLai, 0) AS SoDuCongNo
            FROM dbo.KhachHangs k
            LEFT JOIN (
                SELECT KhachHangId, SUM(ConLai) AS TongConLai
                FROM dbo.HoaDons
                WHERE ConLai > 0
                GROUP BY KhachHangId
            ) hd ON k.KhachHangId = hd.KhachHangId
            ORDER BY k.MaKhachHang ASC;";

        // Tạo và tự động giải phóng kết nối sau khi hoàn thành
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<KhachHang>(sql);
    }

    public async Task<KhachHang?> LayKhachHangTheoIdAsync(int khachHangId)
    {
        const string sql = @"
            SELECT 
                k.KhachHangId,
                k.MaKhachHang,
                k.TenKhachHang,
                k.SoDienThoai,
                k.Email,
                k.DiaChi,
                k.HanMucCongNo,
                k.TrangThai,
                ISNULL(hd.TongConLai, 0) AS SoDuCongNo
            FROM dbo.KhachHangs k
            LEFT JOIN (
                SELECT KhachHangId, SUM(ConLai) AS TongConLai
                FROM dbo.HoaDons
                WHERE ConLai > 0
                GROUP BY KhachHangId
            ) hd ON k.KhachHangId = hd.KhachHangId
            WHERE k.KhachHangId = @khachHangId;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<KhachHang>(sql, new { khachHangId });
    }
}
