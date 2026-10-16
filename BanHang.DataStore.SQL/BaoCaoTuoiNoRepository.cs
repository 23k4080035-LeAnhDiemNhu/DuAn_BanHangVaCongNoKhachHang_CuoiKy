using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;

namespace BanHang.DataStore.SQL;

public class BaoCaoTuoiNoRepository : IBaoCaoTuoiNoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public BaoCaoTuoiNoRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DuLieuBaoCaoTuoiNoDto> LayDuLieuBaoCaoAsync(DateTime ngayBaoCao)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
        {
            await dbConn.OpenAsync();
        }
        else if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        // Đọc nhất quán bằng 1 connection và 1 transaction với mức Serializable
        // Đặt RangeS-S lock ngăn chặn hoàn toàn phantom rows (INSERT/UPDATE/DELETE) trong phạm vi đọc
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        var ngayKeTiep = ngayBaoCao.Date.AddDays(1);

        const string sql = @"
            -- 1. Truy vấn công nợ hóa đơn và phân bổ có hiệu lực (tổng hợp CTE trước khi JOIN)
            WITH PhanBoHopLe AS (
                SELECT 
                    p.HoaDonId,
                    SUM(p.SoTienPhanBo) AS TongPhanBo
                FROM dbo.PhanBoThanhToans p
                INNER JOIN dbo.ThanhToans t ON p.ThanhToanId = t.ThanhToanId
                WHERE t.NgayThanhToan < @NgayKeTiep
                  AND p.NgayPhanBo < @NgayKeTiep
                GROUP BY p.HoaDonId
            )
            SELECT 
                k.KhachHangId,
                k.MaKhachHang,
                k.TenKhachHang,
                k.SoDienThoai,
                k.HanMucCongNo,
                h.HoaDonId,
                h.MaHoaDon,
                h.NgayLap,
                h.HanThanhToan,
                h.TongTien,
                ISNULL(pb.TongPhanBo, 0) AS TongDaPhanBoCoHieuLuc
            FROM dbo.KhachHangs k
            LEFT JOIN dbo.HoaDons h ON k.KhachHangId = h.KhachHangId AND h.NgayLap < @NgayKeTiep
            LEFT JOIN PhanBoHopLe pb ON h.HoaDonId = pb.HoaDonId
            ORDER BY k.KhachHangId, h.NgayLap, h.HoaDonId;

            -- 2. Truy vấn tiền thu chưa phân bổ có hiệu lực tại ngày chốt
            WITH PhanBoHopLePT AS (
                SELECT 
                    p.ThanhToanId, 
                    SUM(p.SoTienPhanBo) AS TongDaPhanBo
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
            WHERE t.NgayThanhToan < @NgayKeTiep;";

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new { NgayKeTiep = ngayKeTiep },
            transaction: transaction);

        var rawData = (await multi.ReadAsync<KhachHangHoaDonTuoiNoRawDto>()).ToList();
        var tienChuaPhanBo = await multi.ReadSingleAsync<decimal>();

        transaction.Commit();

        return new DuLieuBaoCaoTuoiNoDto
        {
            RawData = rawData,
            TienThuChuaPhanBo = tienChuaPhanBo
        };
    }

    public async Task<IEnumerable<KhachHangHoaDonTuoiNoRawDto>> LayDuLieuTuoiNoRawAsync(DateTime ngayBaoCao)
    {
        var result = await LayDuLieuBaoCaoAsync(ngayBaoCao);
        return result.RawData;
    }

    public async Task<decimal> LayTongTienThuChuaPhanBoAsync(DateTime ngayBaoCao)
    {
        var result = await LayDuLieuBaoCaoAsync(ngayBaoCao);
        return result.TienThuChuaPhanBo;
    }
}
