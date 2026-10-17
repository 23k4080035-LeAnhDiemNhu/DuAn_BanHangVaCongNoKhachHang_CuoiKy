using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Trợ giúp ghi log chẩn đoán tạm thời cho luồng đăng nhập và xác thực (chỉ trong Development).
/// Ghi đồng thời vào ILogger (Visual Studio Output Debug / Console) và file logs/auth-diagnostic.log.
/// Tuyệt đối KHÔNG ghi mật khẩu, hash, cookie value, token hoặc bí mật.
/// </summary>
public static class AuthDiagnosticLogger
{
    private static readonly object _lock = new();
    private static string? _logFilePath;

    public static void Initialize(string contentRootPath)
    {
        try
        {
            var logDir = Path.Combine(contentRootPath, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            _logFilePath = Path.Combine(logDir, "auth-diagnostic.log");

            // Đánh dấu mốc phiên kiểm thử mới
            lock (_lock)
            {
                File.AppendAllText(_logFilePath, $"=== BẮT ĐẦU PHIÊN CHẨN ĐOÁN: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
            }
        }
        catch
        {
            // Bỏ qua lỗi tạo file log nếu bị giới hạn quyền
        }
    }

    public static void Log(ILogger? logger, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        logger?.LogInformation("{DiagnosticMessage}", line);

        if (_logFilePath != null)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Bỏ qua lỗi ghi file
            }
        }
    }
}
