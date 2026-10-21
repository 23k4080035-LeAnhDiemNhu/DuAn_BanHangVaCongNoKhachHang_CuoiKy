using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BanHang.CoreBusiness;

namespace BanHang.UseCases.EmailNhacNoScreen;

/// <summary>
/// Hợp đồng lưu trữ và điều phối Idempotency phía máy chủ cho tính năng gửi email nhắc nợ.
/// Đăng ký DI là Singleton để đảm bảo đồng bộ trạng thái qua mọi DI Scopes trên cùng một tiến trình.
/// </summary>
public interface IEmailIdempotencyStore
{
    /// <summary>
    /// Kiểm tra kết quả đã hoàn thành trước đó. 
    /// Xác thực bắt buộc: Người thực hiện và PayloadHash phải khớp tuyệt đối.
    /// </summary>
    Task<KetQuaGuiEmailDto?> LayKetQuaDaHoanThanhAsync(string idempotencyKey, string currentUser, string payloadHash);

    /// <summary>
    /// Đăng ký tác vụ đang xử lý (In-Flight). Sử dụng Atomic TaskCompletionSource.
    /// Tuyệt đối KHÔNG gọi delegate gửi email trong dictionary factory.
    /// </summary>
    (bool IsLeader, Task<KetQuaGuiEmailDto> Task) BatDauXuLyInFlight(
        string idempotencyKey,
        string currentUser,
        string payloadHash,
        TaskCompletionSource<KetQuaGuiEmailDto> tcs);

    /// <summary>
    /// Gỡ bỏ tác vụ In-Flight khi xử lý xong (thành công hoặc lỗi)
    /// </summary>
    void KetThucInFlight(string idempotencyKey);

    /// <summary>
    /// Lưu kết quả đã hoàn tất vào cache để phục vụ các lần gọi lặp lại hợp lệ
    /// </summary>
    void LuuKetQuaHoanThanh(string idempotencyKey, string currentUser, string payloadHash, KetQuaGuiEmailDto result);

    /// <summary>
    /// Tính mã băm (hash) của nội dung payload để phát hiện xung đột Idempotency khi cùng key nhưng khác tham số
    /// </summary>
    string TinhPayloadHash(YeuCauGuiEmailDto request);
}

/// <summary>
/// Triển khai Idempotency Store trong bộ nhớ máy chủ (Singleton).
/// Lưu ý giới hạn: Chỉ có hiệu lực trên một tiến trình (.NET AppDomain). 
/// Khi khởi động lại ứng dụng hoặc chạy môi trường cụm đa instance (Scale-out load balancer),
/// cần triển khai bộ lưu trữ phân tán bền vững (Redis/Database Table) để đạt Idempotency bền vững.
/// </summary>
public class MemoryEmailIdempotencyStore : IEmailIdempotencyStore
{
    private class InFlightEntry
    {
        public string IdempotencyKey { get; }
        public string NguoiThucHien { get; }
        public string PayloadHash { get; }
        public Task<KetQuaGuiEmailDto> Task { get; }

        public InFlightEntry(string key, string user, string hash, Task<KetQuaGuiEmailDto> task)
        {
            IdempotencyKey = key;
            NguoiThucHien = user;
            PayloadHash = hash;
            Task = task;
        }
    }

    private class CompletedEntry
    {
        public string IdempotencyKey { get; }
        public string NguoiThucHien { get; }
        public string PayloadHash { get; }
        public KetQuaGuiEmailDto Result { get; }
        public DateTime CompletedAt { get; }

        public CompletedEntry(string key, string user, string hash, KetQuaGuiEmailDto result)
        {
            IdempotencyKey = key;
            NguoiThucHien = user;
            PayloadHash = hash;
            Result = result;
            CompletedAt = DateTime.UtcNow;
        }
    }

    private readonly ConcurrentDictionary<string, InFlightEntry> _inFlight = new();
    private readonly ConcurrentDictionary<string, CompletedEntry> _completed = new();

    public Task<KetQuaGuiEmailDto?> LayKetQuaDaHoanThanhAsync(string idempotencyKey, string currentUser, string payloadHash)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Task.FromResult<KetQuaGuiEmailDto?>(null);

        if (_completed.TryGetValue(idempotencyKey, out var entry))
        {
            // 1. Kiểm tra chủ sở hữu / người dùng: Người gọi phải là người đã thực hiện lượt gửi
            if (!string.Equals(entry.NguoiThucHien, currentUser, StringComparison.OrdinalIgnoreCase))
            {
                throw new QuyTacNghiepVuException(
                    "Yêu cầu gửi email bị từ chối: Khóa IdempotencyKey này thuộc sở hữu của một người dùng khác. Bạn không có quyền truy cập.");
            }

            // 2. Kiểm tra xung đột Payload: Cùng IdempotencyKey nhưng khác nội dung/người nhận/tham số
            if (!string.Equals(entry.PayloadHash, payloadHash, StringComparison.Ordinal))
            {
                throw new QuyTacNghiepVuException(
                    "Yêu cầu gửi email bị từ chối do xung đột Idempotency: Khóa này đã được sử dụng trước đó cho một yêu cầu có nội dung, người nhận hoặc ngày chốt khác.");
            }

            return Task.FromResult<KetQuaGuiEmailDto?>(entry.Result);
        }

        return Task.FromResult<KetQuaGuiEmailDto?>(null);
    }

    public (bool IsLeader, Task<KetQuaGuiEmailDto> Task) BatDauXuLyInFlight(
        string idempotencyKey,
        string currentUser,
        string payloadHash,
        TaskCompletionSource<KetQuaGuiEmailDto> tcs)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKey);
        ArgumentNullException.ThrowIfNull(tcs);

        var newEntry = new InFlightEntry(idempotencyKey, currentUser, payloadHash, tcs.Task);

        while (true)
        {
            if (_inFlight.TryAdd(idempotencyKey, newEntry))
            {
                // Là người tạo thành công (Leader) -> Tiếp tục thực thi logic nghiệp vụ và gọi sender bên ngoài
                return (true, tcs.Task);
            }

            // Đã có một tác vụ khác đang chạy đồng thời (Follower)
            if (_inFlight.TryGetValue(idempotencyKey, out var existing))
            {
                // Kiểm tra xung đột người dùng hoặc payload ngay cả khi đang in-flight
                if (!string.Equals(existing.NguoiThucHien, currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    throw new QuyTacNghiepVuException(
                        "Yêu cầu gửi email bị từ chối: Khóa IdempotencyKey này đang được xử lý đồng thời bởi một người dùng khác.");
                }

                if (!string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal))
                {
                    throw new QuyTacNghiepVuException(
                        "Yêu cầu gửi email bị từ chối do xung đột Idempotency: Đang có một yêu cầu khác cùng IdempotencyKey nhưng khác nội dung đang được xử lý đồng thời.");
                }

                return (false, existing.Task);
            }
        }
    }

    public void KetThucInFlight(string idempotencyKey)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _inFlight.TryRemove(idempotencyKey, out _);
        }
    }

    public void LuuKetQuaHoanThanh(string idempotencyKey, string currentUser, string payloadHash, KetQuaGuiEmailDto result)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _completed[idempotencyKey] = new CompletedEntry(idempotencyKey, currentUser, payloadHash, result);
        }
    }

    public string TinhPayloadHash(YeuCauGuiEmailDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var raw = $"{request.KhachHangId}|{request.NgayChotBaoCao:yyyy-MM-dd}|{request.EmailNguoiNhan?.Trim()}|{request.TieuDe?.Trim()}|{request.NoiDung?.Trim()}|{request.HashDuLieuCongNo}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
