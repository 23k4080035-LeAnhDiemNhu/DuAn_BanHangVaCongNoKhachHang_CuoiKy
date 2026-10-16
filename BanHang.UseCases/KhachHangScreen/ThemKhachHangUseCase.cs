using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.KhachHangScreen;

public interface IThemKhachHangUseCase
{
    Task<TaoKhachHangResult> ExecuteAsync(TaoKhachHangRequest request);
}

public class ThemKhachHangUseCase : IThemKhachHangUseCase
{
    private readonly IKhachHangRepository _khachHangRepo;

    public ThemKhachHangUseCase(IKhachHangRepository khachHangRepo)
    {
        _khachHangRepo = khachHangRepo;
    }

    public async Task<TaoKhachHangResult> ExecuteAsync(TaoKhachHangRequest request)
    {
        var result = new TaoKhachHangResult { ThanhCong = false };

        if (request == null)
        {
            result.ThongBao = "Dữ liệu yêu cầu không hợp lệ.";
            return result;
        }

        // 1. Trim khoảng trắng
        request.MaKhachHang = request.MaKhachHang?.Trim() ?? string.Empty;
        request.TenKhachHang = request.TenKhachHang?.Trim() ?? string.Empty;
        request.SoDienThoai = string.IsNullOrWhiteSpace(request.SoDienThoai) ? null : request.SoDienThoai.Trim();
        request.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        request.DiaChi = string.IsNullOrWhiteSpace(request.DiaChi) ? null : request.DiaChi.Trim();

        // 2. Kiểm tra tính bắt buộc và độ dài
        if (string.IsNullOrWhiteSpace(request.MaKhachHang))
        {
            result.LoiTheoTruong["MaKhachHang"] = "Mã khách hàng là bắt buộc.";
        }
        else if (request.MaKhachHang.Length > 20)
        {
            result.LoiTheoTruong["MaKhachHang"] = "Mã khách hàng không được vượt quá 20 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(request.TenKhachHang))
        {
            result.LoiTheoTruong["TenKhachHang"] = "Tên khách hàng là bắt buộc.";
        }
        else if (request.TenKhachHang.Length > 150)
        {
            result.LoiTheoTruong["TenKhachHang"] = "Tên khách hàng không được vượt quá 150 ký tự.";
        }

        if (!string.IsNullOrEmpty(request.SoDienThoai) && request.SoDienThoai.Length > 20)
        {
            result.LoiTheoTruong["SoDienThoai"] = "Số điện thoại không được vượt quá 20 ký tự.";
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            if (request.Email.Length > 100)
            {
                result.LoiTheoTruong["Email"] = "Email không được vượt quá 100 ký tự.";
            }
            else if (!new EmailAddressAttribute().IsValid(request.Email))
            {
                result.LoiTheoTruong["Email"] = "Định dạng email không hợp lệ.";
            }
        }

        if (!string.IsNullOrEmpty(request.DiaChi) && request.DiaChi.Length > 250)
        {
            result.LoiTheoTruong["DiaChi"] = "Địa chỉ không được vượt quá 250 ký tự.";
        }

        // 3. Kiểm tra hạn mức công nợ
        if (request.HanMucCongNo < 0)
        {
            result.LoiTheoTruong["HanMucCongNo"] = "Hạn mức công nợ không được âm.";
        }
        else if (decimal.Round(request.HanMucCongNo, 2) != request.HanMucCongNo)
        {
            result.LoiTheoTruong["HanMucCongNo"] = "Hạn mức công nợ tối đa 2 chữ số thập phân.";
        }

        if (result.LoiTheoTruong.Count > 0)
        {
            result.ThongBao = "Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại các trường thông tin.";
            return result;
        }

        // 4. Kiểm tra trùng mã khách hàng
        bool daTonTai = await _khachHangRepo.KiemTraTrungMaKhachHangAsync(request.MaKhachHang);
        if (daTonTai)
        {
            result.LoiTheoTruong["MaKhachHang"] = $"Mã khách hàng '{request.MaKhachHang}' đã tồn tại trong hệ thống.";
            result.ThongBao = $"Mã khách hàng '{request.MaKhachHang}' đã được sử dụng. Vui lòng nhập mã khác.";
            return result;
        }

        // 5. Lưu vào DataStore
        return await _khachHangRepo.ThemKhachHangAsync(request);
    }
}
