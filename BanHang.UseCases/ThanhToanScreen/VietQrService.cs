using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BanHang.UseCases.ThanhToanScreen;

/// <summary>
/// Dịch vụ khởi tạo mã VietQR Quick Link và xử lý chuẩn hóa dữ liệu chuyển khoản
/// </summary>
public class VietQrService : IVietQrService
{
    public VietQrSettings Settings { get; }

    public VietQrService(VietQrSettings? settings = null)
    {
        Settings = settings ?? new VietQrSettings();
    }

    public (bool HopLe, string? Loi, long SoTienNguyen) KiemTraSoTienVietQr(decimal soTien)
    {
        if (soTien <= 0)
        {
            return (false, "Số tiền thanh toán phải lớn hơn 0.", 0);
        }

        if (decimal.Truncate(soTien) != soTien)
        {
            return (false, "Số tiền thanh toán chuyển khoản qua VietQR phải là số nguyên (VNĐ), không chứa phần thập phân.", 0);
        }

        if (soTien > 9_999_999_999_999m)
        {
            return (false, "Số tiền thanh toán vượt quá giới hạn tối đa 13 chữ số của VietQR.", 0);
        }

        return (true, null, (long)soTien);
    }

    public string ChuanHoaNoiDung(string maPhieuThu, string maKhachHang)
    {
        string p = maPhieuThu?.Trim() ?? string.Empty;
        string k = maKhachHang?.Trim() ?? string.Empty;

        string raw = $"THU {p} {k}".Trim();
        string boDau = BoDauTiengViet(raw);

        // Giữ lại chỉ chữ và số, thay thế ký tự đặc biệt/dấu câu bằng khoảng trắng
        var sb = new StringBuilder();
        foreach (char ch in boDau)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append(' ');
            }
        }

        // Chuẩn hóa khoảng trắng dư thừa và chuyển sang chữ hoa
        string normalized = Regex.Replace(sb.ToString(), @"\s+", " ").Trim().ToUpperInvariant();

        // Giới hạn độ dài tối đa 50 ký tự theo chuẩn VietQR
        if (normalized.Length > 50)
        {
            normalized = normalized.Substring(0, 50).Trim();
        }

        return normalized;
    }

    public string TaoNoiDungChuyenKhoan(string maPhieuThu, string maKhachHang)
    {
        return ChuanHoaNoiDung(maPhieuThu, maKhachHang);
    }

    public VietQrThanhToanDto TaoVietQr(string maPhieuThu, string maKhachHang, decimal soTien)
    {
        var dto = new VietQrThanhToanDto
        {
            BankId = Settings.BankId,
            BankBin = Settings.BankBin,
            BankName = Settings.BankName,
            AccountNumber = Settings.AccountNumber,
            AccountName = Settings.AccountName
        };

        var checkTien = KiemTraSoTienVietQr(soTien);
        if (!checkTien.HopLe)
        {
            dto.HopLe = false;
            dto.ThongBaoLoi = checkTien.Loi;
            return dto;
        }

        dto.SoTienNguyen = checkTien.SoTienNguyen;
        dto.SoTienDinhDang = dto.SoTienNguyen.ToString("N0", new CultureInfo("vi-VN")) + " đ";
        dto.NoiDungChuyenKhoan = ChuanHoaNoiDung(maPhieuThu, maKhachHang);

        string encodedAddInfo = Uri.EscapeDataString(dto.NoiDungChuyenKhoan);
        string encodedAccountName = Uri.EscapeDataString(Settings.AccountName);

        // Cấu trúc Quick Link:
        // https://img.vietqr.io/image/<BANK_ID>-<ACCOUNT_NO>-<TEMPLATE>.svg?amount=<AMOUNT>&addInfo=<DESCRIPTION>&accountName=<ACCOUNT_NAME>
        dto.QrUrl = $"https://img.vietqr.io/image/{Settings.BankId}-{Settings.AccountNumber}-{Settings.Template}.svg?amount={dto.SoTienNguyen}&addInfo={encodedAddInfo}&accountName={encodedAccountName}";
        dto.HopLe = true;

        return dto;
    }

    public static string BoDauTiengViet(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("Đ", "D")
            .Replace("đ", "d");
    }
}
