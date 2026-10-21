using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BanHang.UseCases.EmailNhacNoScreen;

/// <summary>
/// Snapshot bất biến (immutable) lưu trữ bản nháp email đã được kế toán công nợ phê duyệt tại máy chủ
/// </summary>
public record BanDuyetSnapshot
{
    public string MaPheDuyet { get; init; } = string.Empty;
    public int KhachHangId { get; init; }
    public DateTime NgayChotBaoCao { get; init; }
    public string EmailNguoiNhan { get; init; } = string.Empty;
    public string TieuDe { get; init; } = string.Empty;
    public string NoiDung { get; init; } = string.Empty;
    public string HashDuLieuCongNo { get; init; } = string.Empty;
    public string NguoiDuyet { get; init; } = string.Empty;
    public DateTime ThoiDiemDuyet { get; init; } = DateTime.Now;
    public DateTime ThoiDiemHetHan { get; init; } = DateTime.Now.AddMinutes(30);
    public bool DaSuDung { get; init; } = false;
}

/// <summary>
/// Hợp đồng lưu trữ và tiêu thụ bản duyệt email tại máy chủ
/// </summary>
public interface IDuyetEmailStore
{
    Task<string> LuuBanDuyetAsync(BanDuyetSnapshot snapshot);
    Task<BanDuyetSnapshot?> LayBanDuyetAsync(string maPheDuyet);
    Task<bool> TryConsumeBanDuyetAsync(string maPheDuyet);
    Task DanhDauDaSuDungAsync(string maPheDuyet);
}

/// <summary>
/// Triển khai lưu trữ bản duyệt trong bộ nhớ máy chủ (Thread-safe, tiêu thụ nguyên tử, TTL 30 phút)
/// </summary>
public class MemoryDuyetEmailStore : IDuyetEmailStore
{
    private class DuyetEntry
    {
        public BanDuyetSnapshot Snapshot { get; }
        public DateTime ExpireAt { get; }
        private int _consumedState = 0; // 0: chưa tiêu thụ, 1: đã tiêu thụ

        public DuyetEntry(BanDuyetSnapshot snapshot, DateTime expireAt)
        {
            Snapshot = snapshot;
            ExpireAt = expireAt;
            if (snapshot.DaSuDung)
            {
                _consumedState = 1;
            }
        }

        public bool DaSuDung => Volatile.Read(ref _consumedState) == 1;

        public bool TryConsume()
        {
            return Interlocked.CompareExchange(ref _consumedState, 1, 0) == 0;
        }

        public void MarkConsumed()
        {
            Volatile.Write(ref _consumedState, 1);
        }
    }

    private readonly ConcurrentDictionary<string, DuyetEntry> _store = new();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    public Task<string> LuuBanDuyetAsync(BanDuyetSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var maPheDuyet = !string.IsNullOrWhiteSpace(snapshot.MaPheDuyet)
            ? snapshot.MaPheDuyet
            : Guid.NewGuid().ToString("N");

        var snapshotWithKey = snapshot with
        {
            MaPheDuyet = maPheDuyet,
            ThoiDiemHetHan = snapshot.ThoiDiemHetHan != default ? snapshot.ThoiDiemHetHan : DateTime.Now.Add(DefaultTtl)
        };

        var entry = new DuyetEntry(snapshotWithKey, snapshotWithKey.ThoiDiemHetHan);
        _store[maPheDuyet] = entry;

        return Task.FromResult(maPheDuyet);
    }

    public Task<BanDuyetSnapshot?> LayBanDuyetAsync(string maPheDuyet)
    {
        if (string.IsNullOrWhiteSpace(maPheDuyet)) return Task.FromResult<BanDuyetSnapshot?>(null);

        if (_store.TryGetValue(maPheDuyet, out var entry))
        {
            if (DateTime.Now <= entry.ExpireAt)
            {
                var currentSnapshot = entry.Snapshot with { DaSuDung = entry.DaSuDung };
                return Task.FromResult<BanDuyetSnapshot?>(currentSnapshot);
            }
            _store.TryRemove(maPheDuyet, out _);
        }

        return Task.FromResult<BanDuyetSnapshot?>(null);
    }

    /// <summary>
    /// Tiêu thụ mã duyệt nguyên tử (Atomic Consumption) qua Interlocked.
    /// Đảm bảo chỉ duy nhất một luồng có thể tiêu thụ bản duyệt thành công, ngăn chặn race condition.
    /// </summary>
    public Task<bool> TryConsumeBanDuyetAsync(string maPheDuyet)
    {
        if (string.IsNullOrWhiteSpace(maPheDuyet)) return Task.FromResult(false);

        if (_store.TryGetValue(maPheDuyet, out var entry))
        {
            if (DateTime.Now > entry.ExpireAt)
            {
                _store.TryRemove(maPheDuyet, out _);
                return Task.FromResult(false);
            }

            return Task.FromResult(entry.TryConsume());
        }

        return Task.FromResult(false);
    }

    public Task DanhDauDaSuDungAsync(string maPheDuyet)
    {
        if (!string.IsNullOrWhiteSpace(maPheDuyet) && _store.TryGetValue(maPheDuyet, out var entry))
        {
            entry.MarkConsumed();
        }
        return Task.CompletedTask;
    }
}
