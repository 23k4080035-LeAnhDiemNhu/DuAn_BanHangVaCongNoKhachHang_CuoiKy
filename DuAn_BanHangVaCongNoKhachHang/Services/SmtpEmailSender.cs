using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.UseCases.PluginInterfaces.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Adapter gửi email thật qua giao thức SMTP sử dụng MailKit/MimeKit chính thức.
/// Hỗ trợ TLS tiêu chuẩn, HTML-encode dữ liệu động, và cấu hình bí mật ngoài source code.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IConfiguration configuration,
        ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KetQuaGuiEmailRawDto> GuiEmailAsync(ThongTinEmailGuiDto emailDto, CancellationToken cancellationToken = default)
    {
        if (emailDto == null)
        {
            return new KetQuaGuiEmailRawDto
            {
                ThanhCong = false,
                TrangThai = TrangThaiGuiEmail.ThatBai,
                ThongBaoChiTiet = "Dữ liệu email gửi không được để trống.",
                CheDoGui = "GuiThat"
            };
        }

        string intendedEmail = emailDto.NguoiNhanEmail?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(intendedEmail) || !Regex.IsMatch(intendedEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return new KetQuaGuiEmailRawDto
            {
                ThanhCong = false,
                TrangThai = TrangThaiGuiEmail.ThatBai,
                ThongBaoChiTiet = $"Địa chỉ email người nhận không hợp lệ: '{intendedEmail}'.",
                EmailNhanThucTe = intendedEmail,
                CheDoGui = "GuiThat"
            };
        }

        // Đọc cấu hình SMTP
        string smtpServer = _configuration["EmailSettings:SmtpServer"]?.Trim() ?? string.Empty;
        int smtpPort = int.TryParse(_configuration["EmailSettings:SmtpPort"], out int p) ? p : 587;
        string senderEmail = _configuration["EmailSettings:SenderEmail"]?.Trim() ?? string.Empty;
        string senderName = _configuration["EmailSettings:SenderName"]?.Trim() ?? "Kế toán Công nợ";
        string username = _configuration["EmailSettings:Username"]?.Trim() ?? senderEmail;
        string password = _configuration["EmailSettings:Password"] ?? string.Empty;
        string security = _configuration["EmailSettings:Security"]?.Trim() ?? "StartTls";
        string testRedirect = _configuration["EmailSettings:TestRecipientRedirect"]?.Trim() ?? string.Empty;
        int timeoutSeconds = int.TryParse(_configuration["EmailSettings:TimeoutSeconds"], out int t) ? t : 15;

        // Kiểm tra cấu hình bắt buộc
        if (string.IsNullOrWhiteSpace(smtpServer))
        {
            throw new InvalidOperationException("Chưa cấu hình máy chủ SMTP ('EmailSettings:SmtpServer'). Vui lòng kiểm tra appsettings.json hoặc User Secrets.");
        }
        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException("Chưa cấu hình email gửi ('EmailSettings:SenderEmail').");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình mật khẩu/App Password SMTP ('EmailSettings:Password'). " +
                "Vui lòng thiết lập qua User Secrets ('dotnet user-secrets set \"EmailSettings:Password\" \"...\"') hoặc biến môi trường EMAILSETTINGS__PASSWORD.");
        }

        // Xác định địa chỉ nhận thực tế (nếu có cấu hình chuyển hướng an toàn cho môi trường test)
        string actualRecipient = intendedEmail;
        bool isRedirected = false;
        if (!string.IsNullOrWhiteSpace(testRedirect))
        {
            actualRecipient = testRedirect;
            isRedirected = true;
            _logger.LogInformation(
                "[SMTP-REDIRECT] Áp dụng chuyển hướng kiểm thử: Email dự kiến '{Intended}' được gửi tới '{Actual}'.",
                intendedEmail, actualRecipient);
        }

        // Chọn phương thức bảo mật TLS
        SecureSocketOptions socketOptions = security.ToUpperInvariant() switch
        {
            "SSLONCONNECT" => SecureSocketOptions.SslOnConnect,
            "NONE" => SecureSocketOptions.None,
            "AUTO" => SecureSocketOptions.Auto,
            _ => SecureSocketOptions.StartTls
        };

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(emailDto.TenNguoiNhan, actualRecipient));
            message.Subject = emailDto.TieuDe;

            var bodyBuilder = new BodyBuilder
            {
                // Bản plain text thay thế đầy đủ nội dung kế toán đã duyệt
                TextBody = emailDto.NoiDung,

                // Bản HTML chuyên nghiệp tương thích Gmail bằng bảng và CSS inline 100%
                HtmlBody = EmailHtmlFormatter.TaoHtmlEmail(emailDto, senderName)
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // QUAN TRỌNG: Kiểm tra chứng chỉ TLS bình thường, TUYỆT ĐỐI không bỏ qua xác thực chứng chỉ
            client.Timeout = (int)TimeSpan.FromSeconds(timeoutSeconds).TotalMilliseconds;

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // 1. Giai đoạn kết nối & xác thực: Nếu lỗi tại đây, chắc chắn thư CHƯA được gửi đi
            try
            {
                await client.ConnectAsync(smtpServer, smtpPort, socketOptions, linkedCts.Token);

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    await client.AuthenticateAsync(username, password, linkedCts.Token);
                }
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "[SMTP-TIMEOUT-CONNECT] Quá thời gian chờ khi kết nối/xác thực SMTP ({Timeout}s). Thư chắc chắn chưa gửi.", timeoutSeconds);
                return new KetQuaGuiEmailRawDto
                {
                    ThanhCong = false,
                    TrangThai = TrangThaiGuiEmail.ThatBai,
                    ThongBaoChiTiet = $"Quá thời gian kết nối/xác thực máy chủ SMTP ({timeoutSeconds}s). Thư chưa được gửi đi.",
                    EmailNhanThucTe = actualRecipient,
                    CheDoGui = "GuiThat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMTP-CONNECT-ERROR] Lỗi kết nối/xác thực tới máy chủ SMTP {Server}:{Port}. Thư chắc chắn chưa gửi.", smtpServer, smtpPort);
                return new KetQuaGuiEmailRawDto
                {
                    ThanhCong = false,
                    TrangThai = TrangThaiGuiEmail.ThatBai,
                    ThongBaoChiTiet = $"Lỗi kết nối hoặc xác thực SMTP: {ex.Message}",
                    EmailNhanThucTe = actualRecipient,
                    CheDoGui = "GuiThat"
                };
            }

            // 2. Giai đoạn phát lệnh SendAsync:
            string response;
            try
            {
                response = await client.SendAsync(message, linkedCts.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "[SMTP-TIMEOUT-SEND] Quá thời gian chờ phản hồi sau khi phát lệnh SendAsync ({Timeout}s).", timeoutSeconds);
                return new KetQuaGuiEmailRawDto
                {
                    ThanhCong = false,
                    TrangThai = TrangThaiGuiEmail.ChuaRoKetQua,
                    ThongBaoChiTiet = $"Quá thời gian chờ máy chủ SMTP phản hồi (timeout {timeoutSeconds}s) sau khi phát dữ liệu. Thư có thể đã được tiếp nhận; hệ thống không tự động gửi lại.",
                    EmailNhanThucTe = actualRecipient,
                    CheDoGui = "GuiThat"
                };
            }
            catch (MailKit.Net.Smtp.SmtpCommandException cmdEx) when ((int)cmdEx.StatusCode >= 500)
            {
                // Máy chủ SMTP từ chối dứt khoát với mã lỗi 5xx (ví dụ hộp thư không tồn tại, relay denied)
                _logger.LogError(cmdEx, "[SMTP-REJECTED] Máy chủ SMTP từ chối tiếp nhận thư với mã lỗi {Code}: {Msg}", cmdEx.StatusCode, cmdEx.Message);
                return new KetQuaGuiEmailRawDto
                {
                    ThanhCong = false,
                    TrangThai = TrangThaiGuiEmail.ThatBai,
                    ThongBaoChiTiet = $"Máy chủ SMTP từ chối gửi (Mã {cmdEx.StatusCode}): {cmdEx.Message}",
                    EmailNhanThucTe = actualRecipient,
                    CheDoGui = "GuiThat"
                };
            }
            catch (Exception ex)
            {
                // Các lỗi mạng, đứt socket, giao thức không xác định sau khi đã bắt đầu truyền dữ liệu
                _logger.LogError(ex, "[SMTP-SEND-UNCERTAIN] Gián đoạn mạng hoặc giao thức trong quá trình SendAsync tới '{Recipient}'.", actualRecipient);
                return new KetQuaGuiEmailRawDto
                {
                    ThanhCong = false,
                    TrangThai = TrangThaiGuiEmail.ChuaRoKetQua,
                    ThongBaoChiTiet = $"Gián đoạn kết nối trong lúc truyền dữ liệu SMTP ({ex.Message}). Chưa rõ máy chủ đã tiếp nhận hay chưa; tuyệt đối không tự động gửi lại.",
                    EmailNhanThucTe = actualRecipient,
                    CheDoGui = "GuiThat"
                };
            }

            // 3. SendAsync ĐÃ THÀNH CÔNG: Lỗi DisconnectAsync tuyệt đối KHÔNG được đổi kết quả thành ThatBai
            try
            {
                await client.DisconnectAsync(true, linkedCts.Token);
            }
            catch (Exception disconnectEx)
            {
                _logger.LogWarning(disconnectEx, "[SMTP-DISCONNECT-WARN] Gửi thư thành công nhưng lỗi khi đóng kết nối SMTP: {Msg}", disconnectEx.Message);
            }

            string messageId = message.MessageId ?? $"SMTP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            string thongBao = isRedirected
                ? $"Máy chủ SMTP đã tiếp nhận và chuyển tiếp email tới '{actualRecipient}' (Địa chỉ dự kiến: '{intendedEmail}'). Phản hồi: {response}"
                : $"Máy chủ SMTP đã tiếp nhận và chuyển tiếp email thành công tới '{actualRecipient}'. Phản hồi: {response}";

            _logger.LogInformation(
                "[SMTP-SUCCESS] Đã gửi email thành công. MessageId={MessageId}, To={Recipient}, Subject={Subject}",
                messageId, actualRecipient, emailDto.TieuDe);

            return new KetQuaGuiEmailRawDto
            {
                ThanhCong = true,
                TrangThai = TrangThaiGuiEmail.TiepNhan, // Phân biệt rạch ròi: Tiếp nhận gửi đi, không bảo đảm đã đọc
                MaPhanHoiDichVu = messageId,
                ThongBaoChiTiet = thongBao,
                EmailNhanThucTe = actualRecipient,
                CheDoGui = "GuiThat"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMTP-UNEXPECTED-ERROR] Lỗi không xác định khi chuẩn bị gửi email tới '{Recipient}'.", actualRecipient);
            return new KetQuaGuiEmailRawDto
            {
                ThanhCong = false,
                TrangThai = TrangThaiGuiEmail.ThatBai,
                ThongBaoChiTiet = $"Lỗi không mong muốn trong tiến trình gửi SMTP: {ex.Message}",
                EmailNhanThucTe = actualRecipient,
                CheDoGui = "GuiThat"
            };
        }
    }
}
