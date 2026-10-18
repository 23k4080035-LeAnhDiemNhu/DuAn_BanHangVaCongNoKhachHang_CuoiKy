namespace BanHang.UseCases.HoaDonScreen;

/// <summary>
/// Cài đặt service lưu trạng thái hóa đơn nháp trong bộ nhớ cho mỗi Circuit (Scoped).
/// Không sử dụng biến static hay singleton nhằm đảm bảo mỗi người dùng / circuit hoàn toàn độc lập.
/// </summary>
public class HoaDonNhapStateService : IHoaDonNhapStateService
{
    private HoaDonNhapDto? _draft;

    public bool CoDuLieuNhap => _draft != null && _draft.CoDuLieu;

    public HoaDonNhapDto LayDuLieuNhap()
    {
        if (_draft == null)
        {
            return new HoaDonNhapDto();
        }

        return new HoaDonNhapDto
        {
            KhachHangId = _draft.KhachHangId,
            NgayLap = _draft.NgayLap,
            HanThanhToan = _draft.HanThanhToan,
            GhiChu = _draft.GhiChu,
            ChiTietDongs = _draft.ChiTietDongs.Select(d => new DongHangNhapDto
            {
                SanPhamId = d.SanPhamId,
                SoLuong = d.SoLuong
            }).ToList()
        };
    }

    public void CapNhatNhap(int khachHangId, DateTime ngayLap, DateTime? hanThanhToan, string? ghiChu, IEnumerable<DongHangNhapDto> chiTiets)
    {
        _draft = new HoaDonNhapDto
        {
            KhachHangId = khachHangId,
            NgayLap = ngayLap,
            HanThanhToan = hanThanhToan,
            GhiChu = ghiChu,
            ChiTietDongs = chiTiets?.Select(d => new DongHangNhapDto
            {
                SanPhamId = d.SanPhamId,
                SoLuong = d.SoLuong
            }).ToList() ?? new List<DongHangNhapDto>()
        };
    }

    public void XoaNhap()
    {
        _draft = null;
    }
}
