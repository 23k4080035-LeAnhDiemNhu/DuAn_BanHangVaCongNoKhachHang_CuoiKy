namespace BanHang.UseCases.SanPhamScreen;

public static class SanPhamHelper
{
    /// <summary>
    /// Loại bỏ các số 0 thập phân thừa không cần thiết (90000000.00 -> 90000000, 1500.50 -> 1500.5), giữ nguyên phần lẻ tối đa 2 chữ số
    /// </summary>
    public static decimal ChuanHoaDonGia(decimal value)
    {
        return value / 1.000000000000000000000000000000000m;
    }

    /// <summary>
    /// Kiểm tra kích thước và nội dung file ảnh qua magic bytes (chấp nhận JPEG, PNG, WebP)
    /// </summary>
    public static (bool IsValid, string Extension, string MimeType) KiemTraDinhDangAnh(byte[]? bytes)
    {
        if (bytes == null || bytes.Length < 12)
        {
            return (false, string.Empty, string.Empty);
        }

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return (true, ".jpg", "image/jpeg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return (true, ".png", "image/png");
        }

        // WebP: RIFF (bytes 0..3) ... WEBP (bytes 8..11)
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return (true, ".webp", "image/webp");
        }

        return (false, string.Empty, string.Empty);
    }
}
