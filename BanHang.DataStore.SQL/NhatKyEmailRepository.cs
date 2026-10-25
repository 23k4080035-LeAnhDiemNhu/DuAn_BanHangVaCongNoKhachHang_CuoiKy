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

    public async Task CapNhatTrangThaiGuiAsync(
        int nhatKyId,
        TrangThaiGuiEmail trangThai,
        string? chiTiet,
        string? maPhanHoi,
        string? emailNhanThucTe = null,
        string? cheDoGui = null)
    {
        const string sql = @"
            UPDATE dbo.NhatKyEmailNhacNos
            SET TrangThaiGui = CAST(@TrangThai AS INT),
                ChiTietTrangThai = @ChiTiet,
                MaPhanHoiDichVu = COALESCE(@MaPhanHoi, MaPhanHoiDichVu),
                EmailNhanThucTe = COALESCE(@EmailNhanThucTe, EmailNhanThucTe),
                CheDoGui = COALESCE(@CheDoGui, CheDoGui),
                ThoiDiemGui = GETDATE()
            WHERE NhatKyId = @NhatKyId;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            NhatKyId = nhatKyId,
            TrangThai = trangThai,
            ChiTiet = chiTiet,
            MaPhanHoi = maPhanHoi,
            EmailNhanThucTe = emailNhanThucTe,
            CheDoGui = cheDoGui
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

    public async Task<(IEnumerable<NhatKyEmailNhacNo> DanhSach, int TongSoBanGhi)> LayDanhSachPhanTrangAsync(
        string? tuKhoa,
        DateTime? tuNgay,
        DateTime? denNgay,
        TrangThaiGuiEmail? trangThaiGui,
        string? nguonSoan,
        string? cheDoGui,
        int pageIndex,
        int pageSize)
    {
        int offset = Math.Max(0, (pageIndex - 1) * pageSize);
        int limit = Math.Max(1, Math.Min(pageSize, 100));

        var parameters = new DynamicParameters();
        var whereClauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            string trimmed = tuKhoa.Trim();
            parameters.Add("@TuKhoaPattern", $"%{trimmed}%");
            if (int.TryParse(trimmed, out int idVal))
            {
                parameters.Add("@NhatKyIdVal", idVal);
                whereClauses.Add("(MaKhachHang LIKE @TuKhoaPattern OR TenKhachHang LIKE @TuKhoaPattern OR EmailNguoiNhan LIKE @TuKhoaPattern OR EmailNhanThucTe LIKE @TuKhoaPattern OR NhatKyId = @NhatKyIdVal)");
            }
            else
            {
                whereClauses.Add("(MaKhachHang LIKE @TuKhoaPattern OR TenKhachHang LIKE @TuKhoaPattern OR EmailNguoiNhan LIKE @TuKhoaPattern OR EmailNhanThucTe LIKE @TuKhoaPattern)");
            }
        }

        if (tuNgay.HasValue)
        {
            parameters.Add("@TuNgay", tuNgay.Value.Date);
            whereClauses.Add("ThoiDiemGui >= @TuNgay");
        }

        if (denNgay.HasValue)
        {
            parameters.Add("@DenNgay", denNgay.Value.Date.AddDays(1).AddTicks(-1));
            whereClauses.Add("ThoiDiemGui <= @DenNgay");
        }

        if (trangThaiGui.HasValue)
        {
            parameters.Add("@TrangThaiGui", (int)trangThaiGui.Value);
            whereClauses.Add("TrangThaiGui = @TrangThaiGui");
        }

        if (!string.IsNullOrWhiteSpace(nguonSoan))
        {
            parameters.Add("@NguonSoan", nguonSoan.Trim());
            whereClauses.Add("NguonSoan = @NguonSoan");
        }

        if (!string.IsNullOrWhiteSpace(cheDoGui))
        {
            parameters.Add("@CheDoGui", cheDoGui.Trim());
            whereClauses.Add("CheDoGui = @CheDoGui");
        }

        string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        string countSql = $"SELECT COUNT(1) FROM dbo.NhatKyEmailNhacNos {whereSql};";

        string dataSql = $@"
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
            {whereSql}
            ORDER BY ThoiDiemGui DESC, NhatKyId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", limit);

        using var connection = _connectionFactory.CreateConnection();
        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<NhatKyEmailNhacNo>(dataSql, parameters);
        return (items, totalCount);
    }

    public async Task<NhatKyEmailNhacNo?> LayTheoIdAsync(int nhatKyId)
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
            WHERE NhatKyId = @NhatKyId;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NhatKyEmailNhacNo>(sql, new { NhatKyId = nhatKyId });
    }
}
