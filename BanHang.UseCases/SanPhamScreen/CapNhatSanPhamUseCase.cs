using System;
using System.Threading.Tasks;
using BanHang.UseCases.PluginInterfaces.DataStore;

namespace BanHang.UseCases.SanPhamScreen;

public interface ICapNhatSanPhamUseCase
{
    Task<CapNhatSanPhamResult> ExecuteAsync(CapNhatSanPhamRequest request);
}

public class CapNhatSanPhamUseCase : ICapNhatSanPhamUseCase
{
    private readonly ISanPhamRepository _sanPhamRepository;

    public CapNhatSanPhamUseCase(ISanPhamRepository sanPhamRepository)
    {
        _sanPhamRepository = sanPhamRepository ?? throw new ArgumentNullException(nameof(sanPhamRepository));
    }

    public async Task<CapNhatSanPhamResult> ExecuteAsync(CapNhatSanPhamRequest request)
    {
        var result = new CapNhatSanPhamResult { ThanhCong = false };

        if (request == null || request.SanPhamId <= 0)
        {
            result.ThongBao = "Thông tin sản phẩm cần cập nhật không hợp lệ.";
            return result;
        }

        // 1. Trim khoảng trắng
        request.TenSanPham = request.TenSanPham?.Trim() ?? string.Empty;
        request.DonViTinh = request.DonViTinh?.Trim() ?? string.Empty;
        request.MoTa = string.IsNullOrWhiteSpace(request.MoTa) ? null : request.MoTa.Trim();
        request.HinhAnh = string.IsNullOrWhiteSpace(request.HinhAnh) ? null : request.HinhAnh.Trim();

        // 2. Kiểm tra các trường bắt buộc
        if (string.IsNullOrWhiteSpace(request.TenSanPham))
        {
            result.LoiTheoTruong["TenSanPham"] = "Tên sản phẩm không được để trống.";
        }
        else if (request.TenSanPham.Length > 200)
        {
            result.LoiTheoTruong["TenSanPham"] = "Tên sản phẩm tối đa 200 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(request.DonViTinh))
        {
            result.LoiTheoTruong["DonViTinh"] = "Đơn vị tính không được để trống.";
        }
        else if (request.DonViTinh.Length > 50)
        {
            result.LoiTheoTruong["DonViTinh"] = "Đơn vị tính tối đa 50 ký tự.";
        }

        // 3. Kiểm tra độ dài các trường tùy chọn
        if (request.MoTa != null && request.MoTa.Length > 500)
        {
            result.LoiTheoTruong["MoTa"] = "Mô tả tối đa 500 ký tự.";
        }

        if (request.HinhAnh != null && request.HinhAnh.Length > 300)
        {
            result.LoiTheoTruong["HinhAnh"] = "Đường dẫn hình ảnh tối đa 300 ký tự.";
        }

        // 4. Kiểm tra đơn giá
        if (request.DonGia < 0)
        {
            result.LoiTheoTruong["DonGia"] = "Đơn giá không được âm.";
        }
        else if (request.DonGia > 9999999999999999.99m)
        {
            result.LoiTheoTruong["DonGia"] = "Đơn giá vượt quá giới hạn lưu trữ cho phép.";
        }
        else if (decimal.Round(request.DonGia, 2) != request.DonGia)
        {
            result.LoiTheoTruong["DonGia"] = "Đơn giá chỉ được có tối đa 2 chữ số thập phân.";
        }

        if (result.LoiTheoTruong.Count > 0)
        {
            result.ThongBao = "Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại các trường được đánh dấu đỏ.";
            return result;
        }

        // 5. Gửi xuống repository (giữ nguyên SanPhamId, MaSanPham, NgayTao và SoLuongTon)
        return await _sanPhamRepository.CapNhatSanPhamAsync(request);
    }
}
