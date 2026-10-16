using System.Data;
using Microsoft.Data.SqlClient;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Giao diện cung cấp kết nối SQL Server độc lập cho mỗi phiên làm việc
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Tạo một kết nối SqlConnection mới (không dùng chung kết nối lâu dài)
    /// </summary>
    IDbConnection CreateConnection();
}

/// <summary>
/// Lớp triển khai factory tạo kết nối SQL Server sử dụng Microsoft.Data.SqlClient
/// </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Chuỗi kết nối SQL Server không được để trống.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <summary>
    /// Luôn trả về một instance SqlConnection mới cho mỗi lời gọi
    /// </summary>
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
