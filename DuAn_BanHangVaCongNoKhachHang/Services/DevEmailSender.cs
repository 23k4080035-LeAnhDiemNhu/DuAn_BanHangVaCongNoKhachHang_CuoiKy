using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Dịch vụ gửi email môi trường Dev Sandbox / Giả lập.
/// Tuyệt đối KHÔNG phát email thật ra Internet trong lượt triển khai hiện tại.
/// Phân biệt rõ trạng thái Gửi thử (GuiThu) và Tiếp nhận (TiepNhan).
/// </summary>
public class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;
    private readonly IConfiguration _configuration;

    public DevEmailSender(
        ILogger<DevEmailSender> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public Task<KetQuaGuiEmailRawDto> GuiEmailAsync(ThongTinEmailGuiDto emailDto, CancellationToken cancellationToken = default)
    {
        if (emailDto == null)
        {
            return Task.FromResult(new KetQuaGuiEmailRawDto
            {
                ThanhCong = false,
                TrangThai = TrangThaiGuiEmail.ThatBai,
                ThongBaoChiTiet = "Thông tin email gửi không được để trống."
            });
        }

        string email = emailDto.NguoiNhanEmail?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return Task.FromResult(new KetQuaGuiEmailRawDto
            {
                ThanhCong = false,
                TrangThai = TrangThaiGuiEmail.ThatBai,
                ThongBaoChiTiet = $"Địa chỉ email người nhận không hợp lệ: '{email}'."
            });
        }

        // Kiểm tra cấu hình xem có cấu hình SMTP thật hay không
        bool isRealSmtpConfigured = !string.IsNullOrWhiteSpace(_configuration["EmailSettings:SmtpServer"]) &&
                                   !string.IsNullOrWhiteSpace(_configuration["EmailSettings:SmtpPort"]);

        string messageId = $"MOCK-MSG-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        _logger.LogInformation(
            "[DEV-EMAIL-SANDBOX] Giả lập gửi email thành công. MessageId={MessageId}, To={Recipient}, Subject={Subject}",
            messageId,
            email,
            emailDto.TieuDe);

        // Theo yêu cầu mục 7 & 9: Trong lượt triển khai này không phát thư thật; trả về trạng thái GuiThu
        // và lưu rõ thông điệp tiếp nhận sandbox an toàn.
        return Task.FromResult(new KetQuaGuiEmailRawDto
        {
            ThanhCong = true,
            TrangThai = isRealSmtpConfigured ? TrangThaiGuiEmail.TiepNhan : TrangThaiGuiEmail.GuiThu,
            MaPhanHoiDichVu = messageId,
            ThongBaoChiTiet = isRealSmtpConfigured
                ? "Dịch vụ email đã tiếp nhận yêu cầu gửi (Chế độ tích hợp)."
                : "Đã gửi thử nghiệm thành công (Sandbox Dev Mode - chưa phát tán email thật ra Internet).",
            EmailNhanThucTe = email
        });
    }
}
