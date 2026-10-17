using System;
using BanHang.UseCases.PluginInterfaces.Security;
using Microsoft.Extensions.Caching.Memory;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Quản lý giới hạn số lần thử sai đăng nhập bằng IMemoryCache có sẵn của ASP.NET Core
/// Tối đa 5 lần thử sai; nếu vượt quá sẽ tạm khóa 15 phút
/// </summary>
public class MemoryLoginRateLimiter : ILoginRateLimiter
{
    private readonly IMemoryCache _cache;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public MemoryLoginRateLimiter() : this(new MemoryCache(new MemoryCacheOptions()))
    {
    }

    public MemoryLoginRateLimiter(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private static string GetCacheKey(string tenDangNhap) => $"login_fail_{tenDangNhap?.Trim().ToLowerInvariant()}";

    public bool IsLockedOut(string tenDangNhap)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap)) return false;

        string key = GetCacheKey(tenDangNhap);
        if (_cache.TryGetValue<int>(key, out int failedCount))
        {
            return failedCount >= MaxFailedAttempts;
        }

        return false;
    }

    public void RecordFailedAttempt(string tenDangNhap)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap)) return;

        string key = GetCacheKey(tenDangNhap);
        int currentCount = 0;
        if (_cache.TryGetValue<int>(key, out int count))
        {
            currentCount = count;
        }

        currentCount++;
        _cache.Set(key, currentCount, LockoutDuration);
    }

    public void ResetAttempts(string tenDangNhap)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap)) return;

        string key = GetCacheKey(tenDangNhap);
        _cache.Remove(key);
    }
}
