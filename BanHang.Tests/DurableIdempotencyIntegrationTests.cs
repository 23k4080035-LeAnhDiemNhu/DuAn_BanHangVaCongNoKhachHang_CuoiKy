using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BanHang.CoreBusiness;
using BanHang.DataStore.SQL;
using BanHang.UseCases.Common;
using BanHang.UseCases.EmailNhacNoScreen;
using BanHang.UseCases.PluginInterfaces.DataStore;
using BanHang.UseCases.PluginInterfaces.Email;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BanHang.Tests;

/// <summary>
/// Kiểm thử tích hợp Idempotency bền vững (Durable Idempotency) trên DB Verify (QuanLyBanHangCongNo_K21_Verify).
/// Sử dụng fake sender để đảm bảo:
/// 1. Đồng thời hai scope/kết nối -> chỉ gọi sender đúng 1 lần.
/// 2. Retry sau khi tạo lại service -> trả về phản hồi idempotent từ CSDL, sender không bị gọi lại.
/// 3. Cùng IdempotencyKey nhưng khác payload -> báo lỗi xung đột (QuyTacNghiepVuException).
/// 4. Bản ghi ở trạng thái DangXuLy hoặc ChuaRoKetQua -> chặn gửi lại, ném ngoại lệ nghiệp vụ.
/// CHỐT AN TOÀN: Tuyệt đối không chạm vào DB chính QuanLyBanHangCongNo.
/// </summary>
public class DurableIdempotencyIntegrationTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string VerifyDatabaseName = "QuanLyBanHangCongNo_K21_Verify";
    private const string VerifyConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={VerifyDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private readonly string _scriptsDir;
    private readonly ISqlConnectionFactory _connectionFactory;

    public DurableIdempotencyIntegrationTests()
    {
        var baseDir = AppContext.BaseDirectory;
        var dirInfo = new DirectoryInfo(baseDir);
        string? foundDir = null;
        while (dirInfo != null)
        {
            var candidate = Path.Combine(dirInfo.FullName, "BanHang.DataStore.SQL", "Scripts");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "01_Safe_CreateSchema.sql")))
            {
                foundDir = candidate;
                break;
            }
            dirInfo = dirInfo.Parent;
        }

        _scriptsDir = foundDir ?? Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "BanHang.DataStore.SQL", "Scripts"));
        _connectionFactory = new SqlConnectionFactory(VerifyConnectionString);
    }

    private void AssertSafeDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var db = builder.InitialCatalog;

        if (string.Equals(db, "QuanLyBanHangCongNo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("CHỐT AN TOÀN VI PHẠM: Tuyệt đối không thao tác trên CSDL chính QuanLyBanHangCongNo!");
        }

        if (string.Equals(db, "master", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(db, "tempdb", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(db, "model", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(db, "msdb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"CHỐT AN TOÀN VI PHẠM: Không thao tác ghi trên CSDL hệ thống '{db}'!");
        }

        if (!db.EndsWith("_K21_Verify", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"CHỐT AN TOÀN VI PHẠM: Chỉ cho phép thao tác trên database QuanLyBanHangCongNo_K21_Verify. Database hiện tại: '{db}'");
        }
    }

    private IEnumerable<string> SplitSqlBatches(string script)
    {
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var b in batches)
        {
            var trimmed = b.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                yield return trimmed;
            }
        }
    }

    private async Task ExecuteScriptFileAsync(SqlConnection conn, string fileName)
    {
        var filePath = Path.Combine(_scriptsDir, fileName);
        if (!File.Exists(filePath)) return;

        var scriptContent = await File.ReadAllTextAsync(filePath);
        var batches = SplitSqlBatches(scriptContent);

        foreach (var batch in batches)
        {
            await conn.ExecuteAsync(batch);
        }
    }

    public async Task InitializeAsync()
    {
        AssertSafeDatabase(VerifyConnectionString);

        // 1. Đảm bảo DB Verify tồn tại
        using (var masterConn = new SqlConnection(MasterConnectionString))
        {
            await masterConn.OpenAsync();
            var sqlCheckDb = $@"
                IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{VerifyDatabaseName}')
                BEGIN
                    CREATE DATABASE [{VerifyDatabaseName}];
                END;";
            await masterConn.ExecuteAsync(sqlCheckDb);
        }

        // 2. Chạy migration schema nếu cần
        using (var verifyConn = new SqlConnection(VerifyConnectionString))
        {
            await verifyConn.OpenAsync();
            await ExecuteScriptFileAsync(verifyConn, "01_Safe_CreateSchema.sql");
            await ExecuteScriptFileAsync(verifyConn, "02_Safe_CreateNguoiDungs.sql");
            await ExecuteScriptFileAsync(verifyConn, "03_Safe_SeedSampleData.sql");
            await ExecuteScriptFileAsync(verifyConn, "Migration_Create_NhatKyEmailNhacNos.sql");
            await ExecuteScriptFileAsync(verifyConn, "Migration_Upgrade_NhatKyEmailNhacNos_DurableIdempotency.sql");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Mock Helpers

    private class MockFakeEmailSender : IEmailSender
    {
        public int SendCallCount = 0;
        public int DelayMs = 0;
        public TrangThaiGuiEmail ResponseStatus = TrangThaiGuiEmail.TiepNhan;

        public async Task<KetQuaGuiEmailRawDto> GuiEmailAsync(ThongTinEmailGuiDto thongTin, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref SendCallCount);
            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs, cancellationToken);
            }

            return new KetQuaGuiEmailRawDto
            {
                ThanhCong = ResponseStatus == TrangThaiGuiEmail.TiepNhan || ResponseStatus == TrangThaiGuiEmail.GuiThu,
                TrangThai = ResponseStatus,
                ThongBaoChiTiet = "Thư đã được SMTP server tiếp nhận xử lý (Fake)",
                MaPhanHoiDichVu = $"FAKE-SMTP-{Guid.NewGuid():N}"
            };
        }
    }

    private class MockXemDuLieuUseCase : IXemDuLieuNhacNoKhachHangUseCase
    {
        public string HashToReturn { get; set; } = "MOCK-HASH-CONG-NO-VALID";

        public Task<DuLieuKhachHangNhacNoDto> ExecuteAsync(int khachHangId, DateTime ngayChotBaoCao)
        {
            return Task.FromResult(new DuLieuKhachHangNhacNoDto
            {
                KhachHangId = khachHangId,
                MaKhachHang = "KH01",
                TenKhachHang = "Công ty TNHH May Mặc An Phú",
                Email = "ketoan@anphu.vn",
                NgayChotBaoCao = ngayChotBaoCao,
                HanMucCongNo = 100_000_000m,
                TongCongNo = 15_000_000m,
                HashDuLieuCongNo = HashToReturn
            });
        }
    }

    private (YeuCauGuiEmailDto Request, BanDuyetSnapshot Snapshot) TaoYeuCauVaBanDuyet(
        string idempotencyKey,
        string maPheDuyet,
        string user = "ktcn",
        string hash = "MOCK-HASH-CONG-NO-VALID")
    {
        var ngayChot = new DateTime(2026, 10, 20);
        var snapshot = new BanDuyetSnapshot
        {
            MaPheDuyet = maPheDuyet,
            KhachHangId = 1,
            NgayChotBaoCao = ngayChot,
            EmailNguoiNhan = "ketoan@anphu.vn",
            TieuDe = "Nhắc nợ kỳ kế toán 20/10/2026",
            NoiDung = "Kính gửi quý khách thông tin công nợ kỳ 20/10/2026...",
            HashDuLieuCongNo = hash,
            NguoiDuyet = user,
            ThoiDiemDuyet = DateTime.Now,
            ThoiDiemHetHan = DateTime.Now.AddMinutes(30),
            DaSuDung = false
        };

        var request = new YeuCauGuiEmailDto
        {
            KhachHangId = 1,
            NgayChotBaoCao = ngayChot,
            EmailNguoiNhan = "ketoan@anphu.vn",
            TieuDe = "Nhắc nợ kỳ kế toán 20/10/2026",
            NoiDung = "Kính gửi quý khách thông tin công nợ kỳ 20/10/2026...",
            GiongVan = GiongVanEmail.TieuChuan,
            CheDoGui = "MoPhong",
            NguonSoan = "AI",
            DaDuyet = true,
            MaPheDuyet = maPheDuyet,
            HashDuLieuCongNo = hash,
            IdempotencyKey = idempotencyKey
        };

        return (request, snapshot);
    }

    #endregion

    [Fact(DisplayName = "Test 1: Đồng thời hai scope/kết nối -> Chỉ gọi sender đúng 1 lần")]
    public async Task Test1_ConcurrentRequests_TwoScopes_OnlyExecutesSenderOnce()
    {
        // Arrange
        var testKey = $"CONCUR-{Guid.NewGuid():N}";
        var maPheDuyet = $"APPROVAL-{Guid.NewGuid():N}";
        var (request, snapshot) = TaoYeuCauVaBanDuyet(testKey, maPheDuyet);

        var sharedIdempotencyStore = new MemoryEmailIdempotencyStore();
        var sharedDuyetStore = new MemoryDuyetEmailStore();
        await sharedDuyetStore.LuuBanDuyetAsync(snapshot);

        var fakeSender = new MockFakeEmailSender { DelayMs = 60 };
        var xemDuLieuUseCase = new MockXemDuLieuUseCase();

        // Scope 1
        var repoScope1 = new NhatKyEmailRepository(new SqlConnectionFactory(VerifyConnectionString));
        var useCaseScope1 = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repoScope1,
            sharedDuyetStore,
            sharedIdempotencyStore,
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        // Scope 2
        var repoScope2 = new NhatKyEmailRepository(new SqlConnectionFactory(VerifyConnectionString));
        var useCaseScope2 = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repoScope2,
            sharedDuyetStore,
            sharedIdempotencyStore,
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        // Act: Kích hoạt đồng thời cả 2 scope
        var task1 = useCaseScope1.ExecuteAsync(request);
        var task2 = useCaseScope2.ExecuteAsync(request);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.All(results, r => Assert.True(r.ThanhCong));
        Assert.Equal(1, fakeSender.SendCallCount); // Sender chỉ được gọi đúng 1 lần!

        // Kiểm tra trong CSDL Verify: Chỉ có duy nhất 1 bản ghi với IdempotencyKey này
        using var conn = new SqlConnection(VerifyConnectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.NhatKyEmailNhacNos WHERE IdempotencyKey = @Key;",
            new { Key = testKey });
        Assert.Equal(1, count);
    }

    [Fact(DisplayName = "Test 2: Retry sau khi tạo lại service -> Trả về kết quả từ DB, sender không bị gọi lại")]
    public async Task Test2_Retry_AfterServiceReinstantiation_LoadsDurableIdempotentResponse_SenderNotCalledAgain()
    {
        // Arrange
        var testKey = $"RETRY-{Guid.NewGuid():N}";
        var maPheDuyet = $"APPROVAL-{Guid.NewGuid():N}";
        var (request, snapshot) = TaoYeuCauVaBanDuyet(testKey, maPheDuyet);

        var duyetStore1 = new MemoryDuyetEmailStore();
        await duyetStore1.LuuBanDuyetAsync(snapshot);
        var idempotencyStore1 = new MemoryEmailIdempotencyStore();

        var fakeSender = new MockFakeEmailSender { ResponseStatus = TrangThaiGuiEmail.TiepNhan };
        var xemDuLieuUseCase = new MockXemDuLieuUseCase();

        // Lần 1: Service instance 1 thực thi lần đầu
        var repo1 = new NhatKyEmailRepository(_connectionFactory);
        var useCase1 = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repo1,
            duyetStore1,
            idempotencyStore1,
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        var result1 = await useCase1.ExecuteAsync(request);
        Assert.True(result1.ThanhCong);
        Assert.Equal(TrangThaiGuiEmail.TiepNhan, result1.TrangThai);
        Assert.Equal(1, fakeSender.SendCallCount);

        // Act: Tạo lại service hoàn toàn mới (mô phỏng app restart / request mới với bộ nhớ cache trống)
        var freshIdempotencyStore2 = new MemoryEmailIdempotencyStore(); // Cache in-memory hoàn toàn trống!
        var freshDuyetStore2 = new MemoryDuyetEmailStore(); // Store bản duyệt hoàn toàn mới
        var repo2 = new NhatKyEmailRepository(_connectionFactory);

        var useCase2 = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repo2,
            freshDuyetStore2,
            freshIdempotencyStore2,
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        // Retry gửi lại cùng request
        var result2 = await useCase2.ExecuteAsync(request);

        // Assert: Trả về kết quả Idempotent từ CSDL bền vững, sender tuyệt đối không bị gọi lại lần 2
        Assert.True(result2.ThanhCong);
        Assert.Equal(TrangThaiGuiEmail.TiepNhan, result2.TrangThai);
        Assert.Equal(1, fakeSender.SendCallCount); // Vẫn là 1, không tăng lên!
    }

    [Fact(DisplayName = "Test 3: Cùng IdempotencyKey nhưng khác Payload -> Báo lỗi xung đột QuyTacNghiepVuException")]
    public async Task Test3_SameKey_DifferentPayload_ThrowsQuyTacNghiepVuException()
    {
        // Arrange
        var testKey = $"CONFLICT-{Guid.NewGuid():N}";
        var maPheDuyet = $"APPROVAL-{Guid.NewGuid():N}";
        var (request, snapshot) = TaoYeuCauVaBanDuyet(testKey, maPheDuyet);

        var duyetStore = new MemoryDuyetEmailStore();
        await duyetStore.LuuBanDuyetAsync(snapshot);
        var idempotencyStore = new MemoryEmailIdempotencyStore();

        var fakeSender = new MockFakeEmailSender();
        var xemDuLieuUseCase = new MockXemDuLieuUseCase();
        var repo = new NhatKyEmailRepository(_connectionFactory);

        var useCase = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repo,
            duyetStore,
            idempotencyStore,
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        // Lần 1: Thành công
        var result1 = await useCase.ExecuteAsync(request);
        Assert.True(result1.ThanhCong);
        Assert.Equal(1, fakeSender.SendCallCount);

        // Act & Assert: Gửi lại cùng testKey nhưng sửa đổi Tiêu đề / Nội dung (khác payload)
        var alteredRequest = new YeuCauGuiEmailDto
        {
            KhachHangId = request.KhachHangId,
            NgayChotBaoCao = request.NgayChotBaoCao,
            EmailNguoiNhan = request.EmailNguoiNhan,
            TieuDe = "Tiêu đề đã bị can thiệp sửa đổi trái phép",
            NoiDung = request.NoiDung,
            GiongVan = request.GiongVan,
            CheDoGui = request.CheDoGui,
            NguonSoan = request.NguonSoan,
            DaDuyet = true,
            MaPheDuyet = request.MaPheDuyet,
            HashDuLieuCongNo = request.HashDuLieuCongNo,
            IdempotencyKey = testKey // Cùng key!
        };

        // Tạo service mới để kiểm tra đối soát trực tiếp từ CSDL bền vững
        var freshUseCase = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repo,
            new MemoryDuyetEmailStore(),
            new MemoryEmailIdempotencyStore(),
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        var ex = await Assert.ThrowsAsync<QuyTacNghiepVuException>(() => freshUseCase.ExecuteAsync(alteredRequest));
        Assert.Contains("xung đột Idempotency", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, fakeSender.SendCallCount); // Sender không được gọi thêm!
    }

    [Fact(DisplayName = "Test 4: Bản ghi trạng thái DangXuLy hoặc ChuaRoKetQua -> Tuyệt đối không tự gửi lại")]
    public async Task Test4_DangXuLy_Or_ChuaRoKetQua_DoesNotResend()
    {
        var repo = new NhatKyEmailRepository(_connectionFactory);
        var xemDuLieuUseCase = new MockXemDuLieuUseCase();
        var fakeSender = new MockFakeEmailSender();

        // --- Trường hợp 1: DangXuLy ---
        var keyDangXuLy = $"DANGXULY-{Guid.NewGuid():N}";
        var (reqDangXuLy, snapDangXuLy) = TaoYeuCauVaBanDuyet(keyDangXuLy, $"APP-DXL-{Guid.NewGuid():N}");

        var storeDangXuLy = new MemoryEmailIdempotencyStore();
        var hashDangXuLy = storeDangXuLy.TinhPayloadHash(reqDangXuLy);

        // Insert trực tiếp bản ghi DangXuLy vào CSDL Verify
        await repo.LuuNhatKyAsync(new NhatKyEmailNhacNo
        {
            KhachHangId = reqDangXuLy.KhachHangId,
            MaKhachHang = "KH01",
            TenKhachHang = "Công ty TNHH May Mặc An Phú",
            EmailNguoiNhan = reqDangXuLy.EmailNguoiNhan,
            NguoiDuyet = "ktcn",
            ThoiDiemDuyet = DateTime.Now,
            ThoiDiemGui = DateTime.Now,
            NgayChotBaoCao = reqDangXuLy.NgayChotBaoCao,
            TieuDe = reqDangXuLy.TieuDe,
            NoiDung = reqDangXuLy.NoiDung,
            GiongVan = reqDangXuLy.GiongVan,
            TrangThaiGui = TrangThaiGuiEmail.DangXuLy, // Đang xử lý
            ChiTietTrangThai = "Yêu cầu đang được xử lý trong tiến trình khác...",
            IdempotencyKey = keyDangXuLy,
            MaPheDuyet = reqDangXuLy.MaPheDuyet,
            PayloadHash = hashDangXuLy,
            CheDoGui = "MoPhong",
            NguonSoan = "AI"
        });

        var useCaseDangXuLy = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repo,
            new MemoryDuyetEmailStore(),
            new MemoryEmailIdempotencyStore(),
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        var exDangXuLy = await Assert.ThrowsAsync<QuyTacNghiepVuException>(() => useCaseDangXuLy.ExecuteAsync(reqDangXuLy));
        Assert.Contains("đang được xử lý", exDangXuLy.Message);
        Assert.Equal(0, fakeSender.SendCallCount); // Sender không được gọi!

        // --- Trường hợp 2: ChuaRoKetQua ---
        var keyChuaRo = $"CHUARO-{Guid.NewGuid():N}";
        var (reqChuaRo, snapChuaRo) = TaoYeuCauVaBanDuyet(keyChuaRo, $"APP-CR-{Guid.NewGuid():N}");

        var storeChuaRo = new MemoryEmailIdempotencyStore();
        var hashChuaRo = storeChuaRo.TinhPayloadHash(reqChuaRo);

        // Insert trực tiếp bản ghi ChuaRoKetQua vào CSDL Verify
        await repo.LuuNhatKyAsync(new NhatKyEmailNhacNo
        {
            KhachHangId = reqChuaRo.KhachHangId,
            MaKhachHang = "KH01",
            TenKhachHang = "Công ty TNHH May Mặc An Phú",
            EmailNguoiNhan = reqChuaRo.EmailNguoiNhan,
            NguoiDuyet = "ktcn",
            ThoiDiemDuyet = DateTime.Now,
            ThoiDiemGui = DateTime.Now,
            NgayChotBaoCao = reqChuaRo.NgayChotBaoCao,
            TieuDe = reqChuaRo.TieuDe,
            NoiDung = reqChuaRo.NoiDung,
            GiongVan = reqChuaRo.GiongVan,
            TrangThaiGui = TrangThaiGuiEmail.ChuaRoKetQua, // Chưa rõ kết quả (timeout/ngắt mạng)
            ChiTietTrangThai = "Mất kết nối hoặc timeout sau khi phát lệnh gửi. Tuyệt đối không tự động gửi lại.",
            IdempotencyKey = keyChuaRo,
            MaPheDuyet = reqChuaRo.MaPheDuyet,
            PayloadHash = hashChuaRo,
            CheDoGui = "MoPhong",
            NguonSoan = "AI"
        });

        var useCaseChuaRo = new GuiEmailNhacNoUseCase(
            xemDuLieuUseCase,
            fakeSender,
            repo,
            new MemoryDuyetEmailStore(),
            new MemoryEmailIdempotencyStore(),
            FakeCurrentUserService.KeToanCongNo("ktcn"));

        var exChuaRo = await Assert.ThrowsAsync<QuyTacNghiepVuException>(() => useCaseChuaRo.ExecuteAsync(reqChuaRo));
        Assert.Contains("Chưa rõ kết quả", exChuaRo.Message);
        Assert.Contains("không tự động gửi lại", exChuaRo.Message);
        Assert.Equal(0, fakeSender.SendCallCount); // Sender tuyệt đối không được gọi!
    }
}
