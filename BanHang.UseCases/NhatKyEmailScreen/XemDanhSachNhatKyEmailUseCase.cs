using System;
using System.Linq;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.NhatKyEmailScreen;

public class XemDanhSachNhatKyEmailUseCase : IXemDanhSachNhatKyEmailUseCase
{
    private readonly INhatKyEmailRepository _nhatKyRepo;
    private readonly ICurrentUserService _currentUserService;

    public XemDanhSachNhatKyEmailUseCase(
        INhatKyEmailRepository nhatKyRepo,
        ICurrentUserService currentUserService)
    {
        _nhatKyRepo = nhatKyRepo ?? throw new ArgumentNullException(nameof(nhatKyRepo));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<KetQuaDanhSachNhatKyEmailDto> ExecuteAsync(BoLocNhatKyEmailDto boLoc)
    {
        ArgumentNullException.ThrowIfNull(boLoc);

        // 1. Phân quyền nghiệp vụ máy chủ: Chỉ Kế toán công nợ
        await AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);

        // 2. Chuẩn hóa phân trang
        int pageIndex = Math.Max(1, boLoc.PageIndex);
        int pageSize = boLoc.PageSize <= 0 ? 10 : Math.Clamp(boLoc.PageSize, 1, 100);

        // 3. Gọi repository thực thi truy vấn Dapper tham số hóa
        var (danhSachEntities, tongSoBanGhi) = await _nhatKyRepo.LayDanhSachPhanTrangAsync(
            boLoc.TuKhoa,
            boLoc.TuNgay,
            boLoc.DenNgay,
            boLoc.TrangThaiGui,
            boLoc.NguonSoan,
            boLoc.CheDoGui,
            pageIndex,
            pageSize);

        var items = danhSachEntities.Select(MapToDto).ToList();

        return new KetQuaDanhSachNhatKyEmailDto
        {
            DanhSach = items,
            TongSoBanGhi = tongSoBanGhi,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public static NhatKyEmailItemDto MapToDto(NhatKyEmailNhacNo entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new NhatKyEmailItemDto
        {
            NhatKyId = entity.NhatKyId,
            KhachHangId = entity.KhachHangId,
            MaKhachHang = entity.MaKhachHang,
            TenKhachHang = entity.TenKhachHang,
            EmailNguoiNhan = entity.EmailNguoiNhan,
            EmailNhanThucTe = entity.EmailNhanThucTe,
            NguoiDuyet = entity.NguoiDuyet,
            ThoiDiemDuyet = entity.ThoiDiemDuyet,
            ThoiDiemGui = entity.ThoiDiemGui,
            NgayChotBaoCao = entity.NgayChotBaoCao,
            TieuDe = entity.TieuDe,
            NoiDung = entity.NoiDung,
            GiongVan = entity.GiongVan,
            TrangThaiGui = entity.TrangThaiGui,
            ChiTietTrangThai = entity.ChiTietTrangThai,
            MaPhanHoiDichVu = entity.MaPhanHoiDichVu,
            IdempotencyKey = entity.IdempotencyKey,
            MaPheDuyet = entity.MaPheDuyet,
            PayloadHash = entity.PayloadHash,
            CheDoGui = entity.CheDoGui,
            NguonSoan = entity.NguonSoan
        };
    }
}
