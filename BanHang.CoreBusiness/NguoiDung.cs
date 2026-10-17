using System;
using System.ComponentModel.DataAnnotations;

namespace BanHang.CoreBusiness;

/// <summary>
/// Đại diện cho tài khoản người dùng đăng nhập hệ thống
/// </summary>
public class NguoiDung
{
    public int NguoiDungId { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(256)]
    public string MatKhauHash { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vai trò không được để trống")]
    [StringLength(50)]
    public string VaiTro { get; set; } = string.Empty;

    public bool TrangThai { get; set; } = true;

    public DateTime NgayTao { get; set; } = DateTime.Now;
}
