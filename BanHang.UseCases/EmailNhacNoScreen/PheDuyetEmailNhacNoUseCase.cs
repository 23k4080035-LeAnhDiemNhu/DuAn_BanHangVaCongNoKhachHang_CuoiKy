using System;
using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.Common;

namespace BanHang.UseCases.EmailNhacNoScreen;

public class YeuCauDuyetEmailDto
{
    public int KhachHangId { get; set; }
    public DateTime NgayChotBaoCao { get; set; }
    public string EmailNguoiNhan { get; set; } = string.Empty;
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public GiongVanEmail GiongVan { get; set; }
}

public class KetQuaDuyetEmailDto
{
    public bool ThanhCong { get; set; }
    public string MaPheDuyet { get; set; } = string.Empty;
    public string HashDuLieuCongNo { get; set; } = string.Empty;
    public string NguoiDuyet { get; set; } = string.Empty;
    public DateTime ThoiDiemDuyet { get; set; }
    public string? ThongBaoLoi { get; set; }
}

public interface IPheDuyetEmailNhacNoUseCase
{
    Task<KetQuaDuyetEmailDto> ExecuteAsync(YeuCauDuyetEmailDto request, CancellationToken cancellationToken = default);
}

public class PheDuyetEmailNhacNoUseCase : IPheDuyetEmailNhacNoUseCase
{
    private readonly IXemDuLieuNhacNoKhachHangUseCase _xemDuLieuUseCase;
    private readonly IDuyetEmailStore _duyetStore;
    private readonly ICurrentUserService _currentUserService;

    public PheDuyetEmailNhacNoUseCase(
        IXemDuLieuNhacNoKhachHangUseCase xemDuLieuUseCase,
        IDuyetEmailStore duyetStore,
        ICurrentUserService currentUserService)
    {
        _xemDuLieuUseCase = xemDuLieuUseCase ?? throw new ArgumentNullException(nameof(xemDuLieuUseCase));
        _duyetStore = duyetStore ?? throw new ArgumentNullException(nameof(duyetStore));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<KetQuaDuyetEmailDto> ExecuteAsync(YeuCauDuyetEmailDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Phân quyền: Chỉ KeToanCongNo
        await AuthorizeHelper.RequireRoleAsync(_currentUserService, VaiTroConstants.KeToanCongNo);

        // 2. Kiểm tra định dạng email người nhận
        if (!DuLieuKhachHangNhacNoDto.KiemTraEmailHopLe(request.EmailNguoiNhan))
        {
            throw new QuyTacNghiepVuException("Địa chỉ email người nhận không hợp lệ hoặc đang để trống. Không thể duyệt bản nháp.");
        }

        // 3. Kiểm tra tiêu đề và nội dung
        if (string.IsNullOrWhiteSpace(request.TieuDe))
        {
            throw new QuyTacNghiepVuException("Tiêu đề email không được để trống khi duyệt.");
        }

        if (string.IsNullOrWhiteSpace(request.NoiDung))
        {
            throw new QuyTacNghiepVuException("Nội dung email không được để trống khi duyệt.");
        }

        // 4. Lấy dữ liệu công nợ thực tế tại ngày chốt áp dụng
        var duLieuHienTai = await _xemDuLieuUseCase.ExecuteAsync(request.KhachHangId, request.NgayChotBaoCao.Date);

        var nguoiDuyet = await _currentUserService.GetTenDangNhapAsync() ?? "KeToanCongNo";
        var thoiDiemDuyet = DateTime.Now;

        // 5. Lưu snapshot phê duyệt tại máy chủ
        var snapshot = new BanDuyetSnapshot
        {
            KhachHangId = request.KhachHangId,
            NgayChotBaoCao = request.NgayChotBaoCao.Date,
            EmailNguoiNhan = request.EmailNguoiNhan.Trim(),
            TieuDe = request.TieuDe.Trim(),
            NoiDung = request.NoiDung.Trim(),
            HashDuLieuCongNo = duLieuHienTai.HashDuLieuCongNo,
            NguoiDuyet = nguoiDuyet,
            ThoiDiemDuyet = thoiDiemDuyet,
            ThoiDiemHetHan = thoiDiemDuyet.AddMinutes(30),
            DaSuDung = false
        };

        string maPheDuyet = await _duyetStore.LuuBanDuyetAsync(snapshot);

        return new KetQuaDuyetEmailDto
        {
            ThanhCong = true,
            MaPheDuyet = maPheDuyet,
            HashDuLieuCongNo = duLieuHienTai.HashDuLieuCongNo,
            NguoiDuyet = nguoiDuyet,
            ThoiDiemDuyet = thoiDiemDuyet
        };
    }
}
