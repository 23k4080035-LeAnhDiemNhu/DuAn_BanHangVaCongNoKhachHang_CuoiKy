# ĐỒ ÁN CAPSTONE: QUẢN LÝ BÁN HÀNG & CÔNG NỢ KHÁCH HÀNG (ĐỀ SỐ 18)

- **Học phần**: Lập trình Ứng dụng Web / Đồ án Capstone (ECO2415)
- **Nền tảng**: .NET 8 (C#) / ASP.NET Core
- **Mô hình giao diện**: Classic Blazor Server (xem chi tiết đối chiếu tại mục [Lưu ý mô hình Blazor](#luu-y-mo-hinh-blazor))
- **Kiến trúc**: Clean Architecture 4 tầng (`CoreBusiness` → `UseCases` → `DataStore.SQL` / `Reports.Excel` → `DuAn_BanHangVaCongNoKhachHang`)
- **Cơ sở dữ liệu**: Microsoft SQL Server / LocalDB với Dapper (truy vấn tham số hóa 100%)
- **Hệ thống Nghiệp vụ**: Bán hàng B2B, Kiểm soát Hạn mức Tín dụng (Credit Limit), Thu nợ cấn trừ tự động theo thuật toán FIFO, Báo cáo phân tích tuổi nợ 4 nhóm, và Trợ lý AI hỗ trợ soạn email nhắc nợ có phê duyệt.

---

## 1. Cấu Trúc Dự Án Thực Tế (Solution Structure)

Solution `DuAn_BanHangVaCongNoKhachHang.sln` được tổ chức chặt chẽ theo chuẩn Clean Architecture gồm các project sau:

```
DuAn_BanHangVaCongNoKhachHang/
│
├── BanHang.CoreBusiness/              # Tầng Thực thể Miền (Domain Entities & Business Rules)
│   ├── KhachHang.cs                   # Entity Khách hàng, Luật 1 kiểm tra hạn mức tín dụng
│   ├── SanPham.cs                     # Entity Sản phẩm, tồn kho
│   ├── HoaDon.cs                      # Entity Hóa đơn bán chịu, trạng thái thanh toán
│   ├── ChiTietHoaDon.cs               # Dòng hàng chi tiết hóa đơn
│   ├── ThanhToan.cs                   # Phiếu thu tiền
│   ├── PhanBoThanhToan.cs             # Chi tiết cấn trừ nợ
│   ├── NguoiDung.cs                   # Tài khoản và phân quyền
│   ├── NhatKyEmailNhacNo.cs           # Thực thể Audit Log gửi email nhắc nợ
│   ├── ThuatToanFIFO.cs               # Luật 2: Thuật toán cấn trừ nợ FIFO độc lập
│   ├── PhanTichTuoiNoService.cs       # Luật 2: Phân tích 4 nhóm tuổi nợ (0-30, 31-60, 61-90, >90)
│   └── MatKhauHelper.cs               # Thuật toán băm mật khẩu PBKDF2 (HMAC-SHA256)
│   (Hoàn toàn không phụ thuộc vào bất kỳ project nào khác trong solution)
│
├── BanHang.UseCases/                  # Tầng Ca Sử Dụng (Application Business Rules)
│   ├── KhachHangScreen/               # Use cases: Xem, Thêm, Cập nhật, Khóa/Mở khách hàng
│   ├── SanPhamScreen/                 # Use cases: Xem danh mục, Thêm, Sửa sản phẩm
│   ├── HoaDonScreen/                  # Use cases: Xem danh sách, Lập hóa đơn bán chịu, Draft State
│   ├── ThanhToanScreen/               # Use cases: Lập phiếu thu, Xem trước phân bổ FIFO
│   ├── BaoCaoTuoiNoScreen/            # Use cases: Xem báo cáo tuổi nợ, Xuất báo cáo Excel
│   ├── EmailNhacNoScreen/             # Use cases: Soạn email AI, Phê duyệt, Gửi email nhắc nợ
│   ├── AuthScreen/                    # Use cases: Đăng nhập, kiểm tra tài khoản
│   └── PluginInterfaces/              # Port Interfaces (DataStore, Ai, Email, Excel)
│   (Chỉ tham chiếu duy nhất BanHang.CoreBusiness)
│
├── BanHang.DataStore.SQL/             # Tầng Plugin Truy Xuất CSDL (Dapper + SQL Server)
│   ├── HoaDonRepository.cs            # Lưu hóa đơn bán chịu trong Transaction (UPDLOCK, khóa hạn mức)
│   ├── ThanhToanRepository.cs         # Lưu phiếu thu và phân bổ FIFO trong Transaction (sp_getapplock)
│   ├── KhachHangRepository.cs         # Truy vấn và quản lý khách hàng
│   ├── SanPhamRepository.cs           # Truy vấn và quản lý sản phẩm
│   ├── BaoCaoTuoiNoRepository.cs      # Truy vấn dữ liệu nợ phục vụ phân tích tuổi nợ
│   ├── NguoiDungRepository.cs         # Xác thực tài khoản và phân quyền
│   ├── NhatKyEmailRepository.cs       # Ghi nhận và cập nhật audit log email nhắc nợ
│   └── Scripts/                       # Toàn bộ script SQL khởi tạo và migration an toàn
│
├── BanHang.Reports.Excel/             # Tầng Plugin Báo Cáo
│   └── ClosedXmlBaoCaoTuoiNoExcelExporter.cs # Xuất báo cáo tuổi nợ .xlsx định dạng ClosedXML
│
├── DuAn_BanHangVaCongNoKhachHang/     # Tầng Trình Diễn WebApp (Composition Root)
│   ├── Pages/                         # Razor Components & Pages (KhachHang, SanPham, HoaDon, ThanhToan,...)
│   ├── Components/                    # Shared Components tái sử dụng (ModalXacNhan, BadgeTrangThai, TrangThaiDuLieu)
│   ├── Services/                      # Adapters: AiChatClientEmailDraftingService, SmtpEmailSender, DevEmailSender
│   ├── Program.cs                     # Cấu hình DI, Authentication, Authorizations, AI & Email
│   ├── appsettings.json               # Cấu hình ứng dụng
│   └── Properties/launchSettings.json # Các launch profile (Debug, Verify_K21)
│
├── BanHang.Tests/                     # Dự án Kiểm thử Tự động (xUnit)
│   └── 345 unit & integration tests kiểm thử toàn diện nghiệp vụ, CSDL, AI và Idempotency
│
└── docs/uml/                          # Hồ sơ thiết kế UML chuẩn PlantUML (Use Case, Class, Sequence)
```

---

## 2. Yêu Cầu Môi Trường (System Requirements)

- **Hệ điều hành**: Windows 10/11, macOS, hoặc Linux.
- **.NET SDK**: Phiên bản **.NET 8.0 SDK** trở lên (kiểm tra bằng lệnh `dotnet --version`).
- **Cơ sở dữ liệu**:
  - Microsoft SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) — đi kèm sẵn khi cài đặt Visual Studio.
  - Hoặc Microsoft SQL Server 2019/2022 / SQL Server Express.
- **Công cụ khuyến nghị**: Visual Studio 2022 (v17.8+) hoặc Visual Studio Code (kèm C# Dev Kit Extension), SQL Server Management Studio (SSMS) hoặc Azure Data Studio.

---

## 3. Cài Đặt Schema CSDL & Migration Theo Đúng Thứ Tự

Để khởi tạo hoặc nâng cấp CSDL, hãy mở SSMS / Azure Data Studio kết nối tới máy chủ SQL của bạn (ví dụ `(localdb)\MSSQLLocalDB`) và chạy lần lượt **5 tệp script** trong thư mục `BanHang.DataStore.SQL/Scripts/` theo đúng thứ tự sau:

1. **`01_Safe_CreateSchema.sql`**:
   - Khởi tạo 6 bảng nghiệp vụ chính (`KhachHangs`, `SanPhams`, `HoaDons`, `ChiTietHoaDons`, `ThanhToans`, `PhanBoThanhToans`).
   - Cài đặt các khóa chính, khóa ngoại, chỉ mục, cột tính toán PERSISTED (`ConLai`, `ThanhTien`) và các ràng buộc `CHECK` an toàn (không dùng `DROP` phá hủy dữ liệu).
2. **`02_Safe_CreateNguoiDungs.sql`**:
   - Khởi tạo bảng `NguoiDungs` phục vụ xác thực Cookie Authentication và phân quyền vai trò.
3. **`03_Safe_SeedSampleData.sql`**:
   - Nạp dữ liệu mẫu ban đầu (khách hàng, sản phẩm, tồn kho và các hóa đơn bán chịu mẫu) trong một Transaction an toàn, có ánh xạ khóa ngoại động theo Mã nghiệp vụ.
4. **`Migration_Create_NhatKyEmailNhacNos.sql`**:
   - Tạo bảng `NhatKyEmailNhacNos` phục vụ lưu trữ nhật ký audit log gửi email nhắc nợ.
5. **`Migration_Upgrade_NhatKyEmailNhacNos_DurableIdempotency.sql`**:
   - Bổ sung các cột kiểm soát gửi trùng lặp bền vững (`IdempotencyKey`, `PayloadHash`, `NguoiDuyet`, `LanThu`, `ThoiDiemKhoiTao`, `ThoiDiemCapNhat`) và chỉ mục duy nhất `UX_NhatKyEmailNhacNos_IdempotencyKey`.

> [!NOTE]
> Tất cả các script trên đều được thiết kế theo nguyên tắc an toàn (Idempotent): nếu bảng hoặc cột đã tồn tại, script sẽ bỏ qua thay vì báo lỗi hoặc ghi đè làm mất dữ liệu.

---

## 4. Cơ Chế Tạo Tài Khoản & Băm Mật Khẩu

Hệ thống sử dụng cơ chế bảo mật tiêu chuẩn công nghiệp:
- **Thuật toán băm**: PBKDF2 (Password-Based Key Derivation Function 2) với `HMAC-SHA256`, 100,000 vòng lặp (iterations), và 128-bit muối (salt) ngẫu nhiên sinh từ `RandomNumberGenerator` (`BanHang.CoreBusiness.MatKhauHelper.BamMatKhau`).
- **Tuyệt đối không lưu mật khẩu dạng bản rõ (plain text)** trong CSDL.

### Các tài khoản mẫu mặc định trong hệ thống:

| Tên đăng nhập | Vai trò | Quyền hạn nghiệp vụ |
| :--- | :--- | :--- |
| `admin` | `Admin` / `QuanTri` | Quản trị toàn hệ thống |
| `nvbh` | `NhanVienBanHang` | Xem danh mục, lập hóa đơn bán chịu, quản lý thông tin khách hàng & sản phẩm |
| `ktcn` | `KeToanCongNo` | Lập phiếu thu cấn trừ FIFO, xem báo cáo tuổi nợ 4 nhóm, xuất Excel, xem và phê duyệt gửi email nhắc nợ |

### Khởi tạo tài khoản khi chạy lần đầu:
Ứng dụng có sẵn `NguoiDungSeeder.cs` hỗ trợ tự động băm mật khẩu và nạp các tài khoản trên vào CSDL khi biến cấu hình `EnableDemoSeeder` được bật tạm thời trong môi trường `Development`. Ngoài ra, quản trị viên có thể sử dụng phương thức `MatKhauHelper.BamMatKhau(matKhau, out salt)` để sinh hash cho tài khoản mới.

---

## 5. Cấu Hình Mẫu Không Chứa Bí Mật

Toàn bộ các khóa bí mật (API Key, Mật khẩu SMTP) **tuyệt đối không lưu trong file mã nguồn hoặc git**. Khi chạy ở môi trường phát triển, hãy sử dụng **.NET User Secrets** hoặc Biến môi trường.

### 5.1. Chế độ Mặc định (Dev / Mô phỏng — Không cần cấu hình thêm)
Trong `appsettings.json`, các dịch vụ AI và Email mặc định được đặt ở chế độ an toàn:
- `AiSettings:Provider = "Fake"`: Sử dụng `FakeChatClient` nội bộ để soạn nháp mẫu, không cần kết nối mạng hay API key.
- `EmailSettings:Mode = "Dev"`: Sử dụng `DevEmailSender`, chỉ ghi log ra Console và cập nhật audit log, không gửi email thật qua Internet.

### 5.2. Chế độ Thật (Gemini AI & Gmail SMTP)
Để kết nối dịch vụ thật, hãy mở Terminal tại thư mục `DuAn_BanHangVaCongNoKhachHang/` và thực thi:

```bash
# 1. Cấu hình AI Gemini qua User Secrets
dotnet user-secrets set "AiSettings:Provider" "Gemini"
dotnet user-secrets set "AiSettings:ApiKey" "AIzaSy..."                # API key từ Google AI Studio
dotnet user-secrets set "AiSettings:ModelId" "gemini-3.6-flash"        # Ví dụ mô hình Flash GA khả dụng cho project mới có Free Tier (nếu để trống, hệ thống tự dự phòng gemini-3.6-flash)

# 2. Cấu hình Gửi Email SMTP Gmail qua User Secrets
dotnet user-secrets set "EmailSettings:Mode" "Real"
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
dotnet user-secrets set "EmailSettings:SenderEmail" "ketoan.congty@gmail.com"
dotnet user-secrets set "EmailSettings:Username" "ketoan.congty@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "xxxx yyyy zzzz wwww"  # Mật khẩu ứng dụng (App Password 16 ký tự)
dotnet user-secrets set "EmailSettings:Security" "StartTls"

# 3. (Tùy chọn) Chuyển hướng người nhận để test an toàn
dotnet user-secrets set "EmailSettings:TestRecipientRedirect" "email_cua_ban@gmail.com"
```

> [!NOTE]
> - **Ưu tiên cấu hình người dùng**: Hệ thống luôn ưu tiên mô hình người dùng đã cấu hình trong User Secrets (`AiSettings:ModelId`). Giá trị `gemini-3.6-flash` chỉ áp dụng làm dự phòng khi `ModelId` để trống.
> - **Hạn mức**: Hạn mức và quyền truy cập mô hình phụ thuộc project, mô hình và cấp sử dụng; kiểm tra trực tiếp trong Google AI Studio.
> - **Tài liệu chính thức Google AI**: [Google AI Gemini Models Reference](https://ai.google.dev/gemini-api/docs/models/gemini) & [Gemini OpenAI Compatibility](https://ai.google.dev/gemini-api/docs/openai) (đối chiếu ngày 21/10/2026).
> - **Lưu ý kiểm thử**: Kết quả kiểm thử tự động sử dụng mock client nội bộ cách ly, không khẳng định việc gọi API thực tế thành công nếu chưa cung cấp API key và quota hợp lệ từ Google AI Studio.

> [!TIP]
> Khi bật `TestRecipientRedirect`, toàn bộ email sẽ được chuyển hướng an toàn về hòm thư chỉ định để kiểm tra giao diện và dấu tiếng Việt, trong khi audit log vẫn ghi nhận chính xác cả người nhận dự kiến và người nhận thực tế.

---

## 6. Lựa Chọn Profile, Chạy Ứng Dụng & Kiểm Thử

### 6.1. Chạy Ứng Dụng Web
Dự án cung cấp 2 Launch Profile chính trong `launchSettings.json`:

1. **Profile `DuAn_BanHangVaCongNoKhachHang` (CSDL Chính)**:
   - Kết nối CSDL chính: `Server=(localdb)\MSSQLLocalDB;Database=QuanLyBanHangCongNo;...`
   - Lệnh chạy:
     ```bash
     dotnet run --project DuAn_BanHangVaCongNoKhachHang
     ```
2. **Profile `Verify_K21` (CSDL Kiểm chứng Demo)**:
   - Kết nối CSDL riêng: `Server=(localdb)\MSSQLLocalDB;Database=QuanLyBanHangCongNo_K21_Verify;...`
   - Lệnh chạy:
     ```bash
     dotnet run --project DuAn_BanHangVaCongNoKhachHang --launch-profile Verify_K21
     ```

Sau khi chạy, truy cập trình duyệt tại địa chỉ: `https://localhost:7008` hoặc `http://localhost:5127`.

### 6.2. Chạy Kiểm Thử Tự Động (Automated Tests)
Hệ thống kiểm thử tự động được cách ly hoàn toàn trên CSDL test riêng (`QuanLyBanHangCongNo_K21_AutoTest` và `QuanLyBanHangCongNo_Test`), có chốt an toàn cấm tuyệt đối thao tác trên DB chính và DB demo:

```bash
# Chạy kiểm thử trọng tâm tính năng Email nhắc nợ & Audit log (43 unit tests):
dotnet test BanHang.Tests --filter "FullyQualifiedName~EmailNhacNoUnitTests"

# Chạy kiểm thử Idempotency chống gửi trùng CSDL (4 integration tests):
dotnet test BanHang.Tests --filter "FullyQualifiedName~DurableIdempotencyIntegrationTests"

# Chạy kiểm chứng CSDL K21 (4 integration tests):
dotnet test BanHang.Tests --filter "FullyQualifiedName~K21DatabaseVerificationTests"

# Chạy toàn bộ test suite của Solution (345/345 tests):
dotnet test BanHang.Tests
```

---

## 7. Lưu Ý Mô Hình Blazor (Blazor Server vs Blazor Web App)

<a id="luu-y-mo-hinh-blazor"></a>
- **Đề bài Capstone (Mục 2 PDF)**: Ghi nhận mô hình *Blazor Web App, render mode Interactive Server*.
- **Triển khai thực tế của dự án**: Sử dụng kiến trúc **Classic Blazor Server** (`AddServerSideBlazor()`, `MapBlazorHub()`, và trang gốc `_Host.cshtml`).
- **Lý do kỹ thuật**: Kiến trúc Classic Blazor Server tương đương video bài giảng giúp bảo toàn trọn vẹn tính ổn định của pipeline **Cookie Authentication** (`HttpContext.SignInAsync` / `SignOutAsync`), cơ chế scoped state của giỏ hàng/nháp hóa đơn (`IHoaDonNhapStateService`), và phân quyền Razor Components mà không gặp lỗi phân mảnh vòng đời giữa Static SSR và Interactive Server.
- **Tính minh bạch**: Dự án ghi nhận rõ ràng sự khác biệt này và **không tự tuyên bố giảng viên đã chấp nhận thay thế**. Đây là điểm kỹ thuật mở để báo cáo và bảo vệ trực tiếp trước Hội đồng chấm thi.
