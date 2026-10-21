using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.DataStore;
using Dapper;

namespace BanHang.DataStore.SQL;

/// <summary>
/// Triển khai INhatKyEmailRepository ghi nhận và truy vấn lịch sử gửi email nhắc nợ (dbo.NhatKyEmailNhacNos)
/// </summary>
public class NhatKyEmailRepository : INhatKyEmailRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public NhatKyEmailRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<int> LuuNhatKyAsync(NhatKyEmailNhacNo nhatKy)
    {
        if (nhatKy == null) throw new ArgumentNullException(nameof(nhatKy));

        const string sql = @"
            INSERT INTO dbo.NhatKyEmailNhacNos (
                KhachHangId,
                MaKhachHang,
                TenKhachHang,
                EmailNguoiNhan,
                EmailNhanThucTe,
                NguoiDuyet,
                ThoiDiemDuyet,
                ThoiDiemGui,
                NgayChotBaoCao,
                TieuDe,
                NoiDung,
                GiongVan,
                TrangThaiGui,
                ChiTietTrangThai,
                MaPhanHoiDichVu,
                IdempotencyKey,
                MaPheDuyet,
                PayloadHash,
                CheDoGui,
                NguonSoan
            )
            VALUES (
                @KhachHangId,
                @MaKhachHang,
                @TenKhachHang,
                @EmailNguoiNhan,
                @EmailNhanThucTe,
                @NguoiDuyet,
                @ThoiDiemDuyet,
                @ThoiDiemGui,
                @NgayChotBaoCao,
                @TieuDe,
                @NoiDung,
                CAST(@GiongVan AS INT),
                CAST(@TrangThaiGui AS INT),
                @ChiTietTrangThai,
                @MaPhanHoiDichVu,
                @IdempotencyKey,
                @MaPheDuyet,
                @PayloadHash,
                @CheDoGui,
                @NguonSoan
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, nhatKy);
    }

    public async Task<int> GhiNhanYeuCauGuiAsync(NhatKyEmailNhacNo nhatKy)
    {
        if (nhatKy == null) throw new ArgumentNullException(nameof(nhatKy));
        nhatKy.TrangThaiGui = TrangThaiGuiEmail.DangXuLy;
        return await LuuNhatKyAsync(nhatKy);
    }

    public async Task CapNhatTrangThaiGuiAsync(int nhatKyId, TrangThaiGuiEmail trangThai, string? chiTiet, string? maPhanHoi)
    {
        const string sql = @"
            UPDATE dbo.NhatKyEmailNhacNos
            SET TrangThaiGui = CAST(@TrangThai AS INT),
                ChiTietTrangThai = @ChiTiet,
                MaPhanHoiDichVu = COALESCE(@MaPhanHoi, MaPhanHoiDichVu),
                ThoiDiemGui = GETDATE()
            WHERE NhatKyId = @NhatKyId;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            NhatKyId = nhatKyId,
            TrangThai = trangThai,
            ChiTiet = chiTiet,
            MaPhanHoi = maPhanHoi
        });
    }

    public async Task<NhatKyEmailNhacNo?> LayTheoIdempotencyKeyAsync(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;

        const string sql = @"
            SELECT 
                NhatKyId,
                KhachHangId,
                MaKhachHang,
                TenKhachHang,
                EmailNguoiNhan,
                EmailNhanThucTe,
                NguoiDuyet,
                ThoiDiemDuyet,
                ThoiDiemGui,
                NgayChotBaoCao,
                TieuDe,
                NoiDung,
                GiongVan,
                TrangThaiGui,
                ChiTietTrangThai,
                MaPhanHoiDichVu,
                IdempotencyKey,
                MaPheDuyet,
                PayloadHash,
                CheDoGui,
                NguonSoan
            FROM dbo.NhatKyEmailNhacNos
            WHERE IdempotencyKey = @IdempotencyKey;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NhatKyEmailNhacNo>(sql, new { IdempotencyKey = idempotencyKey });
    }

    public async Task<IEnumerable<NhatKyEmailNhacNo>> LayLichSuTheoKhachHangAsync(int khachHangId)
    {
        const string sql = @"
            SELECT 
                NhatKyId,
                KhachHangId,
                MaKhachHang,
                TenKhachHang,
                EmailNguoiNhan,
                EmailNhanThucTe,
                NguoiDuyet,
                ThoiDiemDuyet,
                ThoiDiemGui,
                NgayChotBaoCao,
                TieuDe,
                NoiDung,
                GiongVan,
                TrangThaiGui,
                ChiTietTrangThai,
                MaPhanHoiDichVu,
                IdempotencyKey,
                MaPheDuyet,
                PayloadHash,
                CheDoGui,
                NguonSoan
            FROM dbo.NhatKyEmailNhacNos
            WHERE KhachHangId = @KhachHangId
            ORDER BY ThoiDiemGui DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<NhatKyEmailNhacNo>(sql, new { KhachHangId = khachHangId });
    }
}
