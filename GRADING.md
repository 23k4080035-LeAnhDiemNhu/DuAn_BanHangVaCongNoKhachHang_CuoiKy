# BẢNG ĐÁNH GIÁ VÀ BẰNG CHỨNG ĐỒ ÁN CAPSTONE (GRADING.MD)

- **Môn học**: Lập trình Ứng dụng Web / Đồ án Capstone (ECO2415)
- **Đề tài**: Đề số 18 – Bán hàng & Công nợ Khách hàng (Sales & Accounts Receivable)
- **Nền tảng**: .NET 8 / C#
- **Mô hình Blazor**: Classic Blazor Server (sử dụng `AddServerSideBlazor`, `MapBlazorHub` và `_Host.cshtml`).
  > **Ghi chú đối chiếu công nghệ**: PDF Đề tài Mục 2 ghi nhận mô hình *Blazor Web App, render mode Interactive Server (tương đương Blazor Server trong video)*. Dự án hiện tại đang triển khai theo kiến trúc Classic Blazor Server tương đương video bài giảng, chưa chuyển sang template root mới `AddRazorComponents` của .NET 8 để bảo toàn tính ổn định của pipeline Cookie Authentication.
- **Kiến trúc**: Clean Architecture 4 tầng (`CoreBusiness → UseCases → Plugins/DataStore → WebApp`)
- **Tài liệu tham chiếu**: `capstone-topics-2026.pdf` (Mục 1–3, Mục 7 Đề 18, Mục 10, 11 và 12).

---

## I. BẢNG TỰ ĐÁNH GIÁ VÀ ĐỐI SOÁT CHI TIẾT TỪNG MÃ TIÊU CHÍ (K1.1 – K5.3)

### Nhóm 1: Kiến trúc Clean Architecture & Code Quality (25%)

#### K1.1: Cấu trúc 4 project và Quy tắc phụ thuộc Clean Architecture
- **Yêu cầu của thầy**: Đủ 4 project; CoreBusiness không tham chiếu project nào; UseCases chỉ tham chiếu CoreBusiness.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - `BanHang.CoreBusiness/BanHang.CoreBusiness.csproj`: Dòng 1–9 *(Hoàn toàn không có ProjectReference hoặc PackageReference ngoài)*.
  - `BanHang.UseCases/BanHang.UseCases.csproj`: Dòng 3–5 *(Chỉ tham chiếu duy nhất `..\BanHang.CoreBusiness\BanHang.CoreBusiness.csproj`)*.
  - `BanHang.DataStore.SQL/BanHang.DataStore.SQL.csproj`: Dòng 3–6 *(Chỉ tham chiếu `CoreBusiness` và `UseCases`)*.
  - `DuAn_BanHangVaCongNoKhachHang/DuAn_BanHangVaCongNoKhachHang.csproj`: Dòng 3–7 *(Tham chiếu `CoreBusiness`, `UseCases`, `DataStore.SQL`)*.
- **Bằng chứng kiểm thử / Demo**: Solution build thành công cả 4 project bằng lệnh `dotnet build DuAn_BanHangVaCongNoKhachHang.sln`.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Kiến trúc phân tầng tuân thủ chuẩn Clean Architecture.

---

#### K1.2: Interface Repository và Inversion of Control
- **Yêu cầu của thầy**: Interface repository nằm ở UseCases, Plugin cài đặt; file .razor inject interface, không inject lớp cụ thể.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Thư mục Interface: `BanHang.UseCases/PluginInterfaces/DataStore/` chứa 6 interface: `IHoaDonRepository.cs`, `IKhachHangRepository.cs`, `ISanPhamRepository.cs`, `IThanhToanRepository.cs`, `IBaoCaoTuoiNoRepository.cs`, `INguoiDungRepository.cs`.
  - Cài đặt Repository: Nằm tại plugin `BanHang.DataStore.SQL/`.
  - Composition Root: `DuAn_BanHangVaCongNoKhachHang/Program.cs` dòng 96–126 đăng ký dependency injection qua interface (ví dụ dòng 101, 107, 114, 119, 124). Tên namespace `BanHang.DataStore.SQL` chỉ xuất hiện duy nhất tại `Program.cs`.
  - Tầng giao diện: Tất cả các trang Razor chỉ inject UseCase interface:
    - `DuAn_BanHangVaCongNoKhachHang/Pages/TaoHoaDonPage.razor`: Dòng 9–11 (`@inject IXemDanhSachKhachHangUseCase`, `@inject IXemDanhSachSanPhamHoatDongUseCase`, `@inject ITaoHoaDonBanChiuUseCase`).
    - `DuAn_BanHangVaCongNoKhachHang/Pages/TaoThanhToanPage.razor`: Dòng 8–10 (`@inject IXemDanhSachKhachHangUseCase`, `@inject IXemTruocPhanBoFifoUseCase`, `@inject ITaoThanhToanUseCase`).
    - `DuAn_BanHangVaCongNoKhachHang/Pages/BaoCaoTuoiNoPage.razor`: Dòng 7 (`@inject IXemBaoCaoTuoiNoUseCase`).
- **Bằng chứng kiểm thử / Demo**: Grep toàn bộ thư mục `DuAn_BanHangVaCongNoKhachHang/Pages` không có bất kỳ file `.razor` nào inject trực tiếp class cụ thể của Repository.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có.

---

#### K1.3: Cài đặt 2 Luật nghiệp vụ cốt lõi tại CoreBusiness / UseCases
- **Yêu cầu của thầy**: 2 luật nghiệp vụ của đề cài trong CoreBusiness / UseCases, không nằm trong .razor (3 điểm mỗi luật).
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - **Luật 1 (Kiểm soát hạn mức tín dụng khi lập hóa đơn mới)**:
    - *Định nghĩa nghiệp vụ*: `BanHang.CoreBusiness/KhachHang.cs`: Dòng 60–63 cài đặt method:
      ```csharp
      public bool KiemTraVuotHanMucKhiMua(decimal soTienDonHangMoi) => (SoDuCongNo + soTienDonHangMoi) > HanMucCongNo;
      ```
    - *Thực tế luồng lưu trong transaction*: `BanHang.DataStore.SQL/HoaDonRepository.cs`:
      - Dòng 78–86: Khóa khách hàng theo hàng bằng `WITH (UPDLOCK, HOLDLOCK)`.
      - Dòng 192–196: Đọc số dư công nợ thực tế `congNoHienTai` trong cùng transaction.
      - Dòng 198: Gán đúng vào entity: `khachHang.SoDuCongNo = congNoHienTai;`.
      - Dòng 204–213: Gọi trực tiếp phương thức nghiệp vụ của CoreBusiness để kiểm tra:
        ```csharp
        if (khachHang.KiemTraVuotHanMucKhiMua(tongTienMoi))
        {
            transaction.Rollback();
            return new TaoHoaDonResult
            {
                ThanhCong = false,
                ThongBao = $"Hóa đơn vượt quá hạn mức công nợ của khách hàng. Công nợ hiện tại: {congNoHienTai:N0} đ, Hóa đơn mới: {tongTienMoi:N0} đ, Tổng sau tạo: {congNoHienTai + tongTienMoi:N0} đ, Hạn mức tối đa: {khachHang.HanMucCongNo:N0} đ."
            };
        }
        ```
    - *Giao diện Blazor đối soát trước*: `DuAn_BanHangVaCongNoKhachHang/Pages/TaoHoaDonPage.razor`: Dòng 418 tái sử dụng cùng quy tắc của Core:
      ```csharp
      private bool VuotHanMucHienTai => khachHangHienTai != null && khachHangHienTai.KiemTraVuotHanMucKhiMua(TongTienHoaDonMoi);
      ```
    - *Công thức kiểm soát*: $CurrentDebt + NewInvoiceTotal \le CreditLimit$. Bằng hạn mức được phép; vượt đúng 1 đồng bị từ chối.
  - **Luật 2 (Thuật toán FIFO cấn trừ nợ & Báo cáo tuổi nợ 4 nhóm)**:
    - *Thuật toán FIFO*: `BanHang.CoreBusiness/ThuatToanFIFO.cs`: Dòng 55–130 cài đặt thuật toán độc lập; dòng 107 đảm bảo `tienPhanBo = Math.Min(hd.ConLai, soTienConLai)`, dòng 126 đảm bảo tổng phân bổ $\le$ số tiền thanh toán. Được gọi tại `BanHang.UseCases/ThanhToanScreen/XemTruocPhanBoFifoUseCase.cs`: Dòng 66 và `BanHang.DataStore.SQL/ThanhToanRepository.cs`: Dòng 198.
    - *Báo cáo tuổi nợ*: `BanHang.CoreBusiness/PhanTichTuoiNoService.cs`: Dòng 42–101; dòng 80–96 phân bổ vào 4 nhóm (0–30, 31–60, 61–90, trên 90 ngày), dòng 100 bảo đảm `item.TongCongNo = item.TuoiNo_0_30 + item.TuoiNo_31_60 + item.TuoiNo_61_90 + item.TuoiNo_Tren_90;`. Được gọi tại `BanHang.UseCases/BaoCaoTuoiNoScreen/XemBaoCaoTuoiNoUseCase.cs`: Dòng 69.
- **Bằng chứng kiểm thử / Demo**: 
  - `BanHang.Tests/QuyTacHanMucAndValidationTests.cs`: Dòng 40–62 kiểm thử unit test cho `KiemTraVuotHanMucKhiMua` với các trường hợp dưới, bằng, vượt đúng 1 đồng.
  - `BanHang.Tests/IntegrationTests.cs`:
    - Dòng 200–257: `Test01` kiểm thử lưu đơn hàng dưới hạn mức (thành công).
    - Dòng 260–288: `Test02` kiểm thử vượt hạn mức (bị từ chối, không để lại dữ liệu rác).
    - Dòng 418–476: `Test07` kiểm thử 2 yêu cầu đồng thời cùng 1 khách hàng: chỉ tối đa 1 yêu cầu thành công, không thể vượt hạn mức.
    - Dòng 510–567: `Test09` kiểm thử đơn hàng đúng bằng hạn mức: cho phép lưu thành công, trừ tồn kho.
    - Dòng 570–632: `Test10` kiểm thử đơn hàng vượt đúng 1 đồng: bị từ chối, không tạo hóa đơn, không trừ tồn kho.
  - `BanHang.Tests/ThuatToanFifoTests.cs`: 10 test phân bổ FIFO.
  - `BanHang.Tests/BaoCaoTuoiNoUnitTests.cs`: 7 test phân bổ 4 nhóm tuổi nợ.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Cả 2 luật nghiệp vụ cốt lõi đã được cài đặt và gọi thực sự tại `CoreBusiness` và luồng lưu có khóa của `HoaDonRepository`.

---

#### K1.4: Đăng ký Dependency Injection & Quản lý State giỏ/lựa chọn tạm (Scoped)
- **Yêu cầu của thầy**: DI đăng ký đủ; giỏ/lựa chọn tạm dùng Scoped.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - `DuAn_BanHangVaCongNoKhachHang/Program.cs`:
    - Dòng 96–126 đăng ký Scoped cho toàn bộ Repository và UseCase.
    - Dòng 118 đăng ký dịch vụ quản lý trạng thái nháp:
      ```csharp
      builder.Services.AddScoped<IHoaDonNhapStateService, HoaDonNhapStateService>();
      ```
  - Interface & DTOs trạng thái nháp (độc lập hoàn toàn với SQL và giao diện):
    - `BanHang.UseCases/HoaDonScreen/IHoaDonNhapStateService.cs`: Dòng 1–58 (`DongHangNhapDto`, `HoaDonNhapDto`, `IHoaDonNhapStateService`).
  - Triển khai dịch vụ Scoped:
    - `BanHang.UseCases/HoaDonScreen/HoaDonNhapStateService.cs`: Dòng 1–55 quản lý trạng thái nháp trong bộ nhớ cho circuit; không dùng biến static/singleton; các circuit độc lập hoàn toàn.
  - Tầng giao diện Blazor:
    - `DuAn_BanHangVaCongNoKhachHang/Pages/TaoHoaDonPage.razor`:
      - Dòng 12: `@inject IHoaDonNhapStateService HoaDonNhapStateService`
      - Dòng 15: `@implements IDisposable`
      - Dòng 136, 141, 148: Ràng buộc hai chiều và đồng bộ vào nháp (`@bind:after="DongBoVaoDraft"` cho ngày lập, hạn thanh toán, ghi chú).
      - Dòng 355: Nút "Hủy bỏ" gọi `@onclick="HuyBo"` để xóa nháp rồi điều hướng về danh sách.
      - Dòng 438–481 (`LoadDataAsync`): Khi người dùng quay lại trang trong cùng circuit, dữ liệu nháp (khách hàng, ngày lập, hạn thanh toán, ghi chú, dòng sản phẩm, số lượng) được khôi phục; đồng thời **tải lại danh mục khách hàng và sản phẩm mới nhất từ CSDL** để làm mới đơn giá snapshot, số lượng tồn, số dư nợ và hạn mức thực tế (không dùng entity cũ làm nguồn quyết định).
      - Dòng 466, 471, 477, 511, 538, 542–560, 570–575: Tự động đồng bộ vào draft sau mỗi thao tác chọn khách hàng, thêm/xóa dòng, chọn sản phẩm, đổi số lượng hoặc khi component bị Dispose.
      - Dòng 563–568, 606–608: Khi lưu thành công (`result.ThanhCong`) hoặc khi bấm "Hủy bỏ", gọi `HoaDonNhapStateService.XoaNhap()`. Khi lưu thất bại (vượt hạn mức, đổi giá, thiếu tồn...), giữ nguyên nháp để người dùng tiếp tục chỉnh sửa.
- **Bằng chứng kiểm thử / Demo**:
  - `BanHang.Tests/HoaDonNhapStateServiceTests.cs`: 9 kiểm thử unit test:
    - `Test01`: Ban đầu chưa có nháp, `CoDuLieuNhap` trả về `false`.
    - `Test02`: Cập nhật và khôi phục dữ liệu nháp chính xác đầy đủ các trường.
    - `Test03`: Lưu thành công hoặc Hủy bỏ gọi `XoaNhap` làm sạch trạng thái.
    - `Test04`: Lưu thất bại giữ nguyên dữ liệu nháp để tiếp tục chỉnh sửa.
    - `Test05`: Hai Scope độc lập không chia sẻ dữ liệu, bảo đảm tính cô lập giữa các circuit người dùng khác nhau.
    - `Test06`: Vòng đời Hủy bỏ (`HuyBo() -> XoaNhap() -> Điều hướng -> Dispose()`), bảo đảm `Dispose()` không ghi lại nháp sau khi Hủy.
    - `Test07`: Vòng đời Lưu thành công (`LuuHoaDonAsync() -> XoaNhap() -> Điều hướng -> Dispose()`), bảo đảm `Dispose()` không ghi lại nháp sau khi Lưu thành công.
    - `Test08`: Vòng đời Lưu thất bại (`LuuHoaDonAsync() thất bại -> Điều hướng -> Dispose()`), bảo đảm dữ liệu nháp được bảo tồn nguyên vẹn để sửa lại.
    - `Test09`: Vòng đời điều hướng nội bộ trong cùng circuit (`/hoa-don/tao -> /hoa-don -> /hoa-don/tao`), khôi phục trọn vẹn dữ liệu khách hàng, ngày tháng, ghi chú, sản phẩm và số lượng.
  - `BanHang.Tests/IntegrationTests.cs`:
    - `Test02` (Dòng 259–309) và `Test10` (Dòng 615–663): Cập nhật kiểm thử theo cơ chế cấp mã tự động phía máy chủ (`MaHoaDon = string.Empty`), so sánh trực tiếp tổng số hóa đơn, số hóa đơn của khách hàng, số dòng chi tiết trước và sau; khẳng định khi bị từ chối do vượt hạn mức thì không thêm bất kỳ hóa đơn/chi tiết nào, bảo toàn nguyên vẹn số dư nợ cũ và tồn kho thực tế, không dựa vào mã client giả định.
- **Giới hạn kỹ thuật & Hướng dẫn kiểm chứng điều hướng trình duyệt cho NVBH**:
  - Dịch vụ Scoped trong Blazor Server gắn liền với vòng đời của SignalR Circuit. Nháp được bảo toàn khi điều hướng nội bộ bằng Blazor giữa các trang trong cùng một circuit. Không bảo toàn nếu người dùng F5 tải lại toàn bộ trang, mở tab trình duyệt mới hoặc đăng xuất/đăng nhập lại.
  - Các liên kết mở chi tiết hóa đơn `target="_top"` trên danh sách được giữ nguyên hoạt động; không dùng localStorage hoặc lưu nháp SQL.
  - **Đính chính luồng kiểm chứng trình duyệt cho Nhân viên bán hàng (`nvbh`)**: Thao tác điều hướng hợp lệ theo đúng phân quyền là: `/hoa-don/tao` $\to$ `/hoa-don` (Quay lại danh sách) $\to$ `/hoa-don/tao` (Lập hóa đơn) trong cùng một circuit. Tuyệt đối không điều hướng sang các trang kế toán (`/thanh-toan`, `/bao-cao-tuoi-no`) mà tài khoản `nvbh` không có quyền truy cập (sẽ bị chặn theo tiêu chí K4.2).
  - Thao tác điều hướng trực tiếp trên trình duyệt thực tế: **Chưa xác minh qua browser tự động**.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Tiêu chí K1.4 đã hoàn thành trọn vẹn theo đúng yêu cầu kiến trúc.

---

### Nhóm 2: Cơ sở Dữ liệu & Tính toàn vẹn Dữ liệu (25%)

#### K2.1: Thiết kế CSDL từ 5 bảng, chuẩn 3NF, có script tạo bảng và dữ liệu mẫu
- **Yêu cầu của thầy**: Từ 5 bảng, 3NF, có khóa ngoại; script tạo bảng + dữ liệu mẫu trong repo chạy được.
- **Trạng thái**: **Một phần / Chưa xác minh thực thi trên DB trống**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - CSDL gồm 7 bảng chuẩn 3NF:
    - 6 bảng nghiệp vụ: `BanHang.DataStore.SQL/Scripts/01_CreateDatabaseAndTables.sql`: Dòng 41–180 (`KhachHangs`, `SanPhams`, `HoaDons`, `ChiTietHoaDons`, `ThanhToans`, `PhanBoThanhToans`). Có 2 cặp Header–Line: `HoaDons`–`ChiTietHoaDons` và `ThanhToans`–`PhanBoThanhToans`.
    - 1 bảng tài khoản: `BanHang.DataStore.SQL/Scripts/03_CreateNguoiDungsTable.sql`: Dòng 8–21 (`NguoiDungs`).
  - Script dữ liệu mẫu: `BanHang.DataStore.SQL/Scripts/02_SeedData.sql`: Dòng 38–220 nạp khách hàng, sản phẩm, hóa đơn và phân bổ thanh toán phục vụ đối soát tuổi nợ.
- **Đánh giá trung thực & Cảnh báo an toàn**:
  - Chưa chạy kiểm chứng tự động toàn bộ script `01_...` và `02_...` từ đầu đến cuối trên một instance SQL Server hoàn toàn trống do lệnh cấm ghi DB chính trong đợt rà soát này.
  - Script `01_CreateDatabaseAndTables.sql` (dòng 22–33) có lệnh `USE [QuanLyBanHangCongNo]` và `DROP TABLE` hàng loạt; `02_SeedData.sql` (dòng 19–32) có lệnh `DELETE FROM ...` và `DBCC CHECKIDENT RESEED`. **Nguy cơ phá hủy dữ liệu rất cao nếu chạy nhầm vào database đang có dữ liệu thật.**
  - Thiếu trường "Số ngày nợ" trên bảng `KhachHangs` theo yêu cầu Đề số 18.
- **Phần còn thiếu & Công việc cần thực hiện**:
  - Bổ sung cột `SoNgayNo INT DEFAULT (30)` vào bảng `KhachHangs`.
  - Chuẩn hóa script tạo bảng sang dạng Idempotent (`IF NOT EXISTS`) và gỡ bỏ lệnh DROP TABLE mặc định.

---

#### K2.2: Cặp Header–Line ghi trong 1 Transaction
- **Yêu cầu của thầy**: Cặp header–line ghi trong 1 transaction.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - **Luồng Hóa đơn**: `BanHang.DataStore.SQL/HoaDonRepository.cs`: Dòng 64 mở transaction (`connection.BeginTransaction(IsolationLevel.ReadCommitted)`).
    - Ghi Header: Dòng 283–294 (`INSERT INTO dbo.HoaDons`).
    - Ghi Lines: Dòng 307–319 (`INSERT INTO dbo.ChiTietHoaDons`).
    - Trừ tồn kho: Dòng 323–347 (`UPDATE dbo.SanPhams`).
    - Hoàn tất: Dòng 372 (`transaction.Commit()`). Lỗi bất kỳ bước nào rollback toàn bộ tại dòng 385, 396.
  - **Luồng Thu tiền**: `BanHang.DataStore.SQL/ThanhToanRepository.cs`: Dòng 93 mở transaction.
    - Ghi Header phiếu thu: Dòng 248–261 (`INSERT INTO dbo.ThanhToans`).
    - Ghi Lines phân bổ: Dòng 263–275 (`INSERT INTO dbo.PhanBoThanhToans`).
    - Cập nhật số tiền đã trả trên hóa đơn: Dòng 269–274 (`UPDATE dbo.HoaDons SET DaThanhToan = DaThanhToan + ...`).
    - Hoàn tất: Dòng 278 (`transaction.Commit()`).
- **Bằng chứng kiểm thử / Demo**:
  - `BanHang.Tests/TruTonKhoHoaDonIntegrationTests.cs`: Dòng 248–300 kiểm thử khi 1 sản phẩm thiếu tồn, toàn bộ hóa đơn và chi tiết bị rollback, không có bản ghi mồ côi nào được ghi.
  - `BanHang.Tests/ThanhToanIntegrationTests.cs`: Kiểm thử phân bổ ghi đồng thời phiếu thu và chi tiết phân bổ.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có.

---

#### K2.3: Mọi truy vấn tham số hóa, không nối chuỗi SQL
- **Yêu cầu của thầy**: Mọi truy vấn tham số hóa, không nối chuỗi SQL.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Toàn bộ các file trong `BanHang.DataStore.SQL/` (`HoaDonRepository.cs`, `KhachHangRepository.cs`, `SanPhamRepository.cs`, `ThanhToanRepository.cs`, `BaoCaoTuoiNoRepository.cs`, `NguoiDungRepository.cs`).
  - Toàn bộ câu truy vấn Dapper đều dùng tham số dạng `@ParamName` truyền qua dynamic object `new { ... }`.
  - Không có bất kỳ câu lệnh SQL nào ghép chuỗi qua toán tử `+` hoặc chuỗi nội suy `$"SELECT ...` hay `+ "WHERE`.
- **Bằng chứng kiểm thử / Demo**: Grep toàn bộ project `BanHang.DataStore.SQL` với từ khóa `$"SELECT` và `+ "WHERE` đều trả về 0 kết quả.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có.

---

#### K2.4: Luồng giao dịch chính chạy end-to-end trên SQL Server
- **Yêu cầu của thầy**: Luồng giao dịch chính chạy end-to-end và dữ liệu nằm trong SQL Server.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - `DuAn_BanHangVaCongNoKhachHang/appsettings.json`: Dòng 2–4 cấu hình chuỗi kết nối SQL Server `BanHangCongNo`.
  - `DuAn_BanHangVaCongNoKhachHang/Program.cs`: Dòng 89–91 khởi tạo `SqlConnectionFactory`.
  - Tầng giao diện gọi UseCase -> DataStore.SQL ghi nhận trực tiếp vào các bảng `HoaDons`, `ChiTietHoaDons`, `SanPhams`, `ThanhToans`, `PhanBoThanhToans`.
- **Bằng chứng kiểm thử / Demo**: Có thể tạo hóa đơn hoặc phiếu thu từ UI rồi mở SSMS kiểm tra dữ liệu thực tế tại các bảng tương ứng.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có.

---

### Nhóm 3: Giao diện & Trải nghiệm Người dùng (20%)

#### K3.1: Thành phần (Component) tái sử dụng với Parameter / EventCallback
- **Yêu cầu của thầy**: Ít nhất 3 component tái sử dụng có [Parameter] / EventCallback; dùng trong thư mục Components (hoặc Shared).
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Đã tạo thư mục `DuAn_BanHangVaCongNoKhachHang/Components/` và khai báo `@using DuAn_BanHangVaCongNoKhachHang.Components` trong `_Imports.razor`.
  - **Component 1: `ModalXacNhan.razor`**:
    - File & Số dòng: `DuAn_BanHangVaCongNoKhachHang/Components/ModalXacNhan.razor`: Dòng 42–67.
    - `[Parameter]`: `HienThi` (bool), `TieuDe` (string), `NoiDung` (string?), `ChildContent` (RenderFragment?), `HeaderClass` (string), `KieuNutXacNhan` (string), `TextXacNhan` (string), `TextHuy` (string), `NutXacNhanId` (string), `DangXuLy` (bool).
    - `[Parameter] EventCallback`: `OnXacNhan` (EventCallback), `OnHuy` (EventCallback).
    - *Trách nhiệm*: Hộp thoại modal xác nhận tác vụ nhạy cảm, chống bấm lặp kép (`if (DangXuLy) return;`), tùy biến nội dung linh hoạt qua `RenderFragment`.
    - *Các nơi sử dụng thực tế (ít nhất 2 nơi)*:
      1. `Pages/KhachHangPage.razor`: Dòng 258–285 (Modal Xác nhận Tạm khóa / Mở lại khách hàng, giữ nút ID `btn-xac-nhan-doi-trang-thai`).
      2. `Pages/SanPhamPage.razor`: Dòng 296–323 (Modal Xác nhận Ngừng bán / Mở bán lại sản phẩm, giữ nút ID `btn-xac-nhan-doi-trang-thai-sp`).
  - **Component 2: `BadgeTrangThai.razor`** (kèm CSS isolation `BadgeTrangThai.razor.css`):
    - File & Số dòng: `DuAn_BanHangVaCongNoKhachHang/Components/BadgeTrangThai.razor`: Dòng 14–34.
    - `[Parameter]`: `Text` (string), `Loai` (string), `CssClass` (string?), `Id` (string?), `ChildContent` (RenderFragment?).
    - *Trách nhiệm*: Hiển thị huy hiệu trạng thái (status badge) dạng pill pastel đồng nhất với hệ thống thiết kế giao diện Mẫu A, tự động ánh xạ phân loại semantic sang CSS class tương ứng.
    - *Các nơi sử dụng thực tế (ít nhất 3 nơi)*:
      1. `Pages/KhachHangPage.razor`: Dòng 83, 87, 91 (Trạng thái nợ: Vượt hạn mức, Đang có nợ, Không nợ); dòng 97, 101 (Hoạt động: Đang hoạt động, Tạm khóa).
      2. `Pages/SanPhamPage.razor`: Dòng 115–116 (Trạng thái kinh doanh: Đang bán, Ngừng bán).
      3. `Pages/HoaDonPage.razor`: Dòng 73, 76, 80 (Trạng thái thanh toán: Đã thanh toán, Thanh toán 1 phần, Chưa thanh toán).
  - **Component 3: `TrangThaiDuLieu.razor`**:
    - File & Số dòng: `DuAn_BanHangVaCongNoKhachHang/Components/TrangThaiDuLieu.razor`: Dòng 38–53.
    - `[Parameter]`: `DangTai` (bool), `ThongBaoDangTai` (string), `SpinnerClass` (string), `CoLoi` (bool), `ThongBaoLoi` (string?), `KhongCoDuLieu` (bool), `ThongBaoTrong` (string), `ChildContent` (RenderFragment?).
    - `[Parameter] EventCallback`: `OnThuLai` (EventCallback).
    - *Trách nhiệm*: Quản lý và bao bọc 4 trạng thái nạp dữ liệu chuẩn mực cho trang danh sách: Đang tải (spinner), Có lỗi (alert-danger kèm nút Thử lại), Trống (alert-info), và Dữ liệu sẵn sàng (`ChildContent`).
    - *Các nơi sử dụng thực tế (4 nơi)*:
      1. `Pages/KhachHangPage.razor`: Dòng 40–129 (Danh sách khách hàng, callback `TaiDanhSachKhachHangAsync`).
      2. `Pages/SanPhamPage.razor`: Dòng 40–151 (Danh sách sản phẩm, callback `TaiDanhSachSanPhamAsync`).
      3. `Pages/HoaDonPage.razor`: Dòng 23–99 (Danh sách hóa đơn, callback `TaiDanhSachHoaDonAsync`).
      4. `Pages/ThanhToanPage.razor`: Dòng 22–88 (Danh sách phiếu thu tiền, callback `TaiDanhSachThanhToanAsync`).
- **Bằng chứng kiểm thử / Demo**:
  - `BanHang.Tests/Components/ComponentLogicTests.cs`: 17 test cases tự động kiểm tra logic gọi callback `OnXacNhan`, `OnHuy`, cơ chế chống bấm lặp khi `DangXuLy = true`, callback `OnThuLai` khi nhấn Thử lại, và bảng ánh xạ CSS của `BadgeTrangThai`. Toàn bộ 17 test đều Passed.
  - *Ghi chú kiểm tra giao diện*: Chưa kiểm tra browser trực tiếp (tuân thủ nguyên tắc an toàn, người dùng chỉ cần F5 trên trình duyệt đang chạy để kiểm chứng hiển thị).
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Đáp ứng hoàn chỉnh tiêu chuẩn của rubric.

---

#### K3.2: EditForm + DataAnnotations và Lỗi nhập liệu hiển thị cạnh ô nhập
- **Yêu cầu của thầy**: EditForm + DataAnnotations; lỗi nhập liệu hiển thị cạnh ô nhập.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - `DuAn_BanHangVaCongNoKhachHang/_Imports.razor`: Đã bổ sung `@using System.ComponentModel.DataAnnotations` áp dụng toàn cục cho các component Razor.
  - Cả 4 form Blazor đều sử dụng `<EditForm EditContext="@...">`, `<DataAnnotationsValidator />`, quản lý lỗi UseCase qua `ValidationMessageStore` và hiển thị lỗi cạnh từng trường bằng `<ValidationMessage For="..." />`:
    1. **Form Thêm / Sửa Khách hàng** (`DuAn_BanHangVaCongNoKhachHang/Pages/KhachHangPage.razor`):
       - Bọc modal trong `<EditForm EditContext="@editContextKhachHang" OnValidSubmit="LuuKhachHangAsync">` (Dòng 139–237). Nút Lưu mang `type="submit"`, `disabled="@isSaving"`, nút Hủy và Đóng là `type="button"`. Triển khai `@implements IDisposable` hủy đăng ký event handler `OnFieldChanged` sạch sẽ.
       - Model nhập liệu: `public class FormKhachHangModel` (Dòng 563–587) mang DataAnnotations thực tế: `TenKhachHang` [Required, StringLength(150)], `SoDienThoai` [StringLength(20)] (chỉ kiểm tra độ dài theo schema CSDL `VARCHAR(20)`, không siết thuộc tính `[Phone]`), `Email` [EmailAddress, StringLength(100)], `DiaChi` [StringLength(250)], `HanMucCongNo` [Range(0, double.MaxValue, ErrorMessage = "Hạn mức công nợ không được âm.")]. `MaKhachHang` không áp `[Required]` (cấp tự động bởi hệ thống).
       - Input HTML thường: Sử dụng `@bind:after="() => OnKhachHangFieldChanged(nameof(formModel...))"` chủ động kích hoạt `EditContext.NotifyFieldChanged()`.
       - Hiển thị lỗi cạnh ô nhập: `<ValidationMessage For="@(() => formModel.TenKhachHang)" class="text-danger small mt-1 d-block" />` (và tương tự cho `SoDienThoai`, `Email`, `DiaChi`, `HanMucCongNo`).
       - Tích hợp `ValidationMessageStore` (Dòng 348–370) kết hợp lỗi UseCase vào đúng trường; khi trường thay đổi chỉ dọn lỗi server của trường đó (`messageStore.Clear(fieldId)`), không làm mất lỗi DataAnnotations khi người dùng nhập sai.
    2. **Form Thêm / Sửa Sản phẩm** (`DuAn_BanHangVaCongNoKhachHang/Pages/SanPhamPage.razor`):
       - Bọc modal trong `<EditForm EditContext="@editContextSanPham" OnValidSubmit="LuuSanPhamAsync">` (Dòng 136–249). Nút Lưu mang `type="submit" id="btn-luu-san-pham"`, `disabled="@isSaving"`. Triển khai `@implements IDisposable` (Dòng 19, 345–352).
       - Model nhập liệu: `public class FormSanPhamModel` (Dòng 666–698):
         - `TenSanPham` [Required, StringLength(200)], `DonViTinh` [Required, StringLength(50)].
         - Khôi phục đúng quy tắc đơn giá không âm: `DonGia` [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm.")] (0 là giá trị hợp lệ).
         - `SoLuongTon` [Range(0, 2147483647, ErrorMessage = "Số lượng tồn kho phải là số không âm.")].
         - `MoTa` [StringLength(500)].
         - Đối chiếu schema CSDL: `HinhAnh` [StringLength(300, ErrorMessage = "Đường dẫn hình ảnh tối đa 300 ký tự.")] (khớp đúng cột `NVARCHAR(300)`).
       - Input HTML thường: Gắn `@bind:after="() => OnSanPhamFieldChanged(nameof(formModel...))"` cho `TenSanPham`, `DonViTinh`, `DonGia`, `SoLuongTon`, `MoTa`.
       - Hiển thị lỗi cạnh ô nhập: `<ValidationMessage For="..." />` cho từng trường tương ứng.
       - Tích hợp `ValidationMessageStore` (Dòng 321–337), hủy đăng ký event handler khi khởi tạo lại hoặc khi `Dispose`.
    3. **Form Tạo Hóa đơn Bán chịu** (`DuAn_BanHangVaCongNoKhachHang/Pages/TaoHoaDonPage.razor`):
       - Bọc form trong `<EditForm EditContext="@editContextHoaDon" OnValidSubmit="LuuHoaDonAsync">` (Dòng 78–396). Triển khai `@implements IDisposable` (Dòng 15, 696–708).
       - Giữ nguyên hành vi disabled gốc của nút Lưu: `disabled="@(isSubmitting || VuotHanMucHienTai)"` (Dòng 374).
       - Model nhập liệu: `public class FormTaoHoaDonModel` (Dòng 401–415): `KhachHangId` [Range(1, int.MaxValue)], `NgayLap` [Required], `HanThanhToan` [Required] (kiểu `DateTime?`, bắt buộc chọn, không mặc định), `GhiChu` [StringLength(500)].
       - Input HTML thường: `NgayLap` `@bind:after="OnNgayLapChanged"`, `HanThanhToan` `@bind:after="OnHanThanhToanChanged"`, `GhiChu` `@bind:after="OnGhiChuChanged"`. Tất cả đều kích hoạt `NotifyFieldChanged` và đồng bộ nháp Scoped.
       - Đồng bộ với UseCase/schema: Đã bỏ giới hạn 100.000 số lượng tự thêm ở ô nhập HTML (`min="1"`, bỏ `max="100000"`) và trong `LuuHoaDonAsync` (chỉ kiểm tra `line.SoLuong > 0`, đồng bộ đúng quy tắc UseCase).
       - Hiển thị lỗi cạnh ô nhập: `<ValidationMessage For="..." />` cho `KhachHangId`, `NgayLap`, `HanThanhToan`, `GhiChu`, kèm cảnh báo dòng hàng `loiDanhSachSanPham` trực tiếp dưới bảng hàng hóa.
       - Tích hợp `ValidationMessageStore` (Dòng 460–476), hủy đăng ký `hoaDonFieldChangedHandler` khi khởi tạo lại và trong `Dispose`.
    4. **Form Tạo Phiếu Thu Tiền & Phân bổ FIFO** (`DuAn_BanHangVaCongNoKhachHang/Pages/TaoThanhToanPage.razor`):
       - Bọc form trong `<EditForm EditContext="@editContextThanhToan" OnValidSubmit="LuuThanhToanAsync">` (Dòng 64–301). Triển khai `@implements IDisposable` (Dòng 13, 567–574).
       - Giữ nguyên hành vi disabled gốc của nút Lưu: `disabled="@(isSubmitting || formModel.KhachHangId <= 0 || formModel.SoTien <= 0)"` (Dòng 286).
       - Model nhập liệu: `public class FormTaoThanhToanModel` (Dòng 307–324): `KhachHangId` [Range(1, int.MaxValue)], `NgayThanhToan` [Required], `PhuongThuc` [Required, StringLength(50)], `SoTien` [Range(0.01, double.MaxValue)], `GhiChu` [StringLength(500)].
       - Input HTML thường: Gắn `@bind:after` cho `NgayThanhToan`, `PhuongThuc`, `SoTien`, `GhiChu` kích hoạt `NotifyFieldChanged` và cập nhật bản xem trước phân bổ FIFO.
       - Hiển thị lỗi cạnh ô nhập: `<ValidationMessage For="..." />` cho từng trường.
       - Tích hợp `ValidationMessageStore` (Dòng 345–367, 508–540) ánh xạ lỗi UseCase máy chủ vào đúng trường và hủy đăng ký event handler an toàn.
- **Bằng chứng kiểm thử / Demo**:
  - `BanHang.Tests/Components/FormValidationTests.cs`: 18 test cases tự động kiểm tra DataAnnotations trên 4 model form (`FormKhachHangModel`, `FormSanPhamModel`, `FormTaoHoaDonModel`, `FormTaoThanhToanModel`), bao gồm:
    - `FormSanPhamModel_DonGiaBangKhong_HopLe`: xác nhận 0 là đơn giá hợp lệ.
    - `FormSanPhamModel_DonGiaAm_BaoLoiKhongDuocAm`: xác nhận đơn giá âm bị từ chối.
    - `FormSanPhamModel_HinhAnhQua300KyTu_BaoLoiDoDai`: xác nhận chuỗi ảnh > 300 ký tự bị từ chối đúng schema CSDL.
    - `FormKhachHangModel_SoDienThoai_KhongSietDinhDangPhone_ChiKiemTraDoDai`: xác nhận định dạng số điện thoại không bị siết chặt bởi `[Phone]`.
  - Kết quả chạy kiểm thử toàn solution: **248/248 tests Passed 100%** (trong đó có 230 test trước đó + 18 test mới của `FormValidationTests.cs`). Bao gồm toàn bộ integration test trên DB test `QuanLyBanHangCongNo_Test` (khóa `sp_getapplock` timeout, cấn trừ FIFO, hạn mức tín dụng, trừ tồn kho...).
  - *Ghi chú xác minh giao diện*: Chưa chạy trình duyệt tự động (tuân thủ nguyên tắc an toàn, không can thiệp tiến trình web đang chạy; người dùng kiểm chứng trực tiếp bằng cách F5 trình duyệt hiện có).
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Tiêu chí K3.2 đã được rà soát và đáp ứng trọn vẹn mọi yêu cầu.

---

#### K3.3: Thông báo thân thiện khi vi phạm 2 luật nghiệp vụ
- **Yêu cầu của thầy**: Ca vi phạm 2 luật nghiệp vụ hiện thông báo thân thiện, không văng lỗi.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Vi phạm Hạn mức công nợ: `DuAn_BanHangVaCongNoKhachHang/Pages/TaoHoaDonPage.razor`: Dòng 44–51 hiển thị alert cảnh báo thân thiện, dòng 375–378 ghi chú rõ *"Nút Lưu bị vô hiệu hóa do tổng nợ dự kiến vượt quá hạn mức công nợ..."*. Tầng CSDL tại `BanHang.DataStore.SQL/HoaDonRepository.cs`: Dòng 202–206 trả thông báo chi tiết: *"Hóa đơn vượt quá hạn mức công nợ của khách hàng. Công nợ hiện tại: ... đ, Hóa đơn mới: ... đ, Hạn mức: ... đ"*.
  - Vi phạm Phân bổ FIFO / Thu tiền: `DuAn_BanHangVaCongNoKhachHang/Pages/TaoThanhToanPage.razor`: Dòng 32–40 hiển thị alert thân thiện khi số tiền thu không hợp lệ hoặc vượt ngưỡng.
  - Xử lý khóa tranh chấp: Cả 4 repository xử lý mã lỗi âm của `sp_getapplock` và trả thông báo tiếng Việt rõ ràng, không làm crash circuit SignalR.
- **Bằng chứng kiểm thử / Demo**: Demo nhập đơn hàng có giá trị vượt hạn mức -> Nút lưu bị vô hiệu hóa, nếu cố tình gửi request máy chủ từ chối và hiển thị banner cảnh báo đỏ.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có.

---

#### K3.4: Khả năng tương thích trên màn hình điện thoại (Bề rộng 390px)
- **Yêu cầu của thầy**: Dùng được trên màn hình điện thoại (DevTools, bề rộng 390px).
- **Trạng thái**: **Chưa xác minh**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Mã nguồn đã sử dụng hệ thống lưới Bootstrap 5 (`col-md-6`, `col-12`, `d-flex flex-wrap`, `table-responsive`).
  - Chưa được kiểm nghiệm trực tiếp trên trình duyệt ở bề rộng 390px (chế độ iPhone 12/13/14) trong đợt rà soát chỉ đọc này.
- **Phần còn thiếu & Công việc cần thực hiện**:
  - Mở Chrome DevTools, chọn kích thước 390px, kiểm tra trang `/hoa-don`, `/khach-hang`, `/san-pham`, `/bao-cao-tuoi-no` xem các bảng dài có thanh cuộn ngang mượt mà và modal không bị tràn viền.

---

### Nhóm 4: Bảo mật, Phân quyền & Tính năng Nâng cao / AI (15%)

#### K4.1: Đăng nhập, đăng xuất và Phân quyền tối thiểu 2 vai trò
- **Yêu cầu của thầy**: Đăng nhập, đăng xuất; ít nhất 2 vai trò.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Đăng nhập: `DuAn_BanHangVaCongNoKhachHang/Pages/Login.cshtml.cs`: Dòng 20–154 cài đặt Cookie Authentication với `HttpContext.SignInAsync`.
  - Đăng xuất: `DuAn_BanHangVaCongNoKhachHang/Pages/Logout.cshtml.cs`: Dòng 1–35 với `HttpContext.SignOutAsync`.
  - 2 vai trò chuẩn: `BanHang.CoreBusiness/VaiTroConstants.cs`: Dòng 10–11 định nghĩa `NhanVienBanHang` và `KeToanCongNo`.
  - Bảo mật mật khẩu: `DuAn_BanHangVaCongNoKhachHang/Services/PasswordHasherService.cs` băm muối bằng thuật toán PBKDF2 (HMAC-SHA256, 100.000 vòng lặp). Không lưu mật khẩu thô.
  - Chống Brute-force: `DuAn_BanHangVaCongNoKhachHang/Services/MemoryLoginRateLimiter.cs` khóa tạm thời sau 5 lần đăng nhập thất bại liên tiếp.
- **Bằng chứng kiểm thử / Demo**: 
  - `BanHang.Tests/AuthUnitTests.cs`: 12 test kiểm thử băm mật khẩu và giới hạn thử sai.
  - `BanHang.Tests/AuthIntegrationTests.cs`: Kiểm thử đăng nhập phân quyền theo 2 vai trò.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có.

---

#### K4.2: Bảo vệ đường dẫn trang quản trị khi gõ trực tiếp URL
- **Yêu cầu của thầy**: Tài khoản thường gõ thẳng URL trang quản trị bị chặn.
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Root Authorization: `DuAn_BanHangVaCongNoKhachHang/App.razor`: Dòng 5–30 cấu hình `<AuthorizeRouteView>` và thẻ `<NotAuthorized>` hiển thị màn hình từ chối truy cập (403 Forbidden) khi người dùng không đủ quyền.
  - Trang giao diện:
    - `TaoHoaDonPage.razor`: Dòng 2 `@attribute [Authorize(Roles = VaiTroConstants.NhanVienBanHang)]`.
    - `TaoThanhToanPage.razor`: Dòng 2 `@attribute [Authorize(Roles = VaiTroConstants.KeToanCongNo)]`.
    - `ThanhToanPage.razor`: Dòng 2 `@attribute [Authorize(Roles = VaiTroConstants.KeToanCongNo)]`.
    - `BaoCaoTuoiNoPage.razor`: Dòng 2 `@attribute [Authorize(Roles = VaiTroConstants.KeToanCongNo)]`.
  - Tầng UseCase: `BanHang.UseCases/Common/AuthorizeHelper.cs`: Dòng 1–25 kiểm tra quyền máy chủ trước khi xử lý dữ liệu.
- **Bằng chứng kiểm thử / Demo**: `BanHang.Tests/HttpSecurityIntegrationTests.cs`: Dòng 45–120 kiểm thử tự động đăng nhập vai trò Nhân viên bán hàng nhưng gõ URL `/bao-cao-tuoi-no` hoặc `/thanh-toan` -> bị chặn truy cập.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có.

---

#### K4.3: Tính năng nâng cao: Xuất báo cáo (Excel/PDF) từ dữ liệu thật hoặc Trợ lý AI
- **Yêu cầu của thầy**: Tính năng nâng cao: AI đạt đủ mục 10, hoặc xuất báo cáo (Excel/PDF) từ dữ liệu thật.
- **Trạng thái**: **Thiếu**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Toàn bộ solution hiện chưa cài đặt bất kỳ package xuất Excel (`ClosedXML`, `EPPlus`) hoặc PDF (`QuestPDF`, `iTextSharp`).
  - Chưa cài đặt package `Microsoft.Extensions.AI` hay `IChatClient` theo quy định tại Mục 10 của PDF.
  - Trang `BaoCaoTuoiNoPage.razor` hiện chỉ có nút tra cứu số liệu trên màn hình, chưa có nút xuất tệp.
- **Đánh giá trung thực**: Hoàn toàn chưa triển khai tính năng nâng cao nào.
- **Phần còn thiếu & Công việc cần thực hiện**:
  - Cài đặt thư viện `ClosedXML` và tạo UseCase `XuatBaoCaoTuoiNoExcelUseCase` để xuất dữ liệu bảng tuổi nợ 4 nhóm ra tệp `.xlsx` từ dữ liệu thật; thêm nút "Xuất Excel" trên trang `BaoCaoTuoiNoPage.razor`.

---

### Nhóm 5: Hồ sơ Thiết kế (UML) & Thuyết trình Vấn đáp (15%)

#### K5.1: Sơ đồ thiết kế PlantUML (Use Case, Class, Sequence) khớp với mã nguồn
- **Yêu cầu của thầy**: Use Case, Class, Sequence (PlantUML) khớp tên lớp và luồng trong code.
- **Trạng thái**: **Thiếu**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Tìm kiếm toàn bộ repo không có bất kỳ file sơ đồ nào (`.puml`, `.plantuml`, `.wsd`).
- **Đánh giá trung thực**: Chưa có hồ sơ thiết kế PlantUML lưu trong repository.
- **Phần còn thiếu & Công việc cần thực hiện**:
  - Tạo thư mục `docs/uml/` và viết 3 sơ đồ PlantUML chuẩn xác theo code thực tế:
    1. `usecase.puml`: Sơ đồ Use Case phân định rõ vai trò NhanVienBanHang và KeToanCongNo.
    2. `class_diagram.puml`: Sơ đồ Class mô hình 4 tầng Clean Architecture, các Interface Repository và Entity.
    3. `sequence_tao_hoadon.puml`: Sơ đồ tuần tự thể hiện luồng Tạo hóa đơn bán chịu, kiểm tra hạn mức, trừ tồn kho và cấp mã an toàn qua `sp_getapplock`.

---

#### K5.2: Thuyết trình vấn đáp: Chỉ đúng dòng cài luật và giải thích Dependency Rule
- **Yêu cầu của thầy**: Vấn đáp: mỗi thành viên chỉ đúng dòng cài luật và giải thích Dependency Rule.
- **Trạng thái**: **Chưa xác minh**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Phụ thuộc trực tiếp vào phần trả lời cá nhân của từng thành viên trong buổi bảo vệ với Hội đồng chấm thi.
- **Tài liệu chuẩn bị cho thành viên nhóm**:
  - *Dòng cài luật FIFO*: `BanHang.CoreBusiness/ThuatToanFIFO.cs:55-130`.
  - *Dòng cài luật Báo cáo tuổi nợ 4 nhóm*: `BanHang.CoreBusiness/PhanTichTuoiNoService.cs:42-101`.
  - *Dòng cài luật Hạn mức tín dụng*: `BanHang.CoreBusiness/KhachHang.cs:60-63`, `BanHang.DataStore.SQL/HoaDonRepository.cs:204` và `DuAn_BanHangVaCongNoKhachHang/Pages/TaoHoaDonPage.razor:418`.
  - *Dòng cài đặt trừ tồn kho & cấp mã qua `sp_getapplock`*: `BanHang.DataStore.SQL/HoaDonRepository.cs:220-350`.
  - *Giải thích Dependency Rule*: Mở 4 file `.csproj` chứng minh `CoreBusiness` độc lập hoàn toàn, `UseCases` chỉ nhìn thấy `CoreBusiness`, `DataStore.SQL` đóng vai trò plugin phụ thuộc ngược vào `UseCases`, và `DuAn_BanHangVaCongNoKhachHang` chỉ biết Plugin tại composition root `Program.cs`.

---

#### K5.3: Lịch sử Git commit của các thành viên rải đều trên ít nhất 3 tuần
- **Yêu cầu của thầy**: Mọi thành viên có commit, rải trên ít nhất 3 tuần.
- **Trạng thái**: **Thiếu / Không đạt tiêu chí 3 tuần**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Lệnh kiểm tra tác giả: `git shortlog -sn --all`
    - Kết quả: Duy nhất **1 thành viên** có commit (`13 Lê Ánh Diễm Như`).
  - Lệnh kiểm tra thời gian: `git log --date=short --format="%ad %an <%ae> %s"`
    - Kết quả: Các commit chỉ diễn ra trong vòng 4 ngày từ `2026-10-15` đến `2026-10-18`.
- **Đánh giá trung thực**: Chưa đáp ứng tiêu chí mọi thành viên đều có commit và chưa rải đều qua 3 tuần (Tuần 05, Tuần 06, Tuần 07) theo quy định lộ trình Capstone. Nhóm cam kết giữ nguyên lịch sử Git trung thực, không tạo commit giả mạo.

---

## II. TỔNG HỢP TRẠNG THÁI 18 TIÊU CHÍ

**Thống kê tổng quan 18 tiêu chí:**
- **Đạt**: **12 tiêu chí** (`K1.1`, `K1.2`, `K1.3`, `K1.4`, `K2.2`, `K2.3`, `K2.4`, `K3.1`, `K3.2`, `K3.3`, `K4.1`, `K4.2`)
- **Một phần**: **1 tiêu chí** (`K2.1`)
- **Thiếu (0 điểm)**: **3 tiêu chí** (`K4.3`, `K5.1`, `K5.3`)
- **Chưa xác minh**: **2 tiêu chí** (`K3.4`, `K5.2`)

| Nhóm tiêu chí | Mã | Nội dung | Trạng thái |
| :--- | :--- | :--- | :--- |
| **1. Kiến trúc Clean Architecture (25%)** | **K1.1** | Đủ 4 project, đúng chiều phụ thuộc | **Đạt** |
| | **K1.2** | Interface ở UseCases, Razor inject interface | **Đạt** |
| | **K1.3** | 2 luật nghiệp vụ cài ở CoreBusiness / UseCases | **Đạt** |
| | **K1.4** | DI đăng ký đủ, giỏ/lựa chọn tạm dùng Scoped | **Đạt** |
| **2. CSDL & Toàn vẹn Dữ liệu (25%)** | **K2.1** | CSDL từ 5 bảng, chuẩn 3NF, script chạy được | **Một phần** |
| | **K2.2** | Header–Line ghi trong 1 transaction | **Đạt** |
| | **K2.3** | Mọi truy vấn tham số hóa, không nối chuỗi SQL | **Đạt** |
| | **K2.4** | Luồng giao dịch chính chạy end-to-end trên SQL Server | **Đạt** |
| **3. Giao diện & Trải nghiệm (20%)** | **K3.1** | Ít nhất 3 component tái sử dụng (Parameter/EventCallback) | **Đạt** |
| | **K3.2** | EditForm + DataAnnotations, lỗi cạnh ô nhập | **Đạt** |
| | **K3.3** | Báo lỗi thân thiện khi vi phạm 2 luật nghiệp vụ | **Đạt** |
| | **K3.4** | Tương thích màn hình điện thoại (bề rộng 390px) | **Chưa xác minh** |
| **4. Bảo mật & Nâng cao (15%)** | **K4.1** | Đăng nhập, đăng xuất, ít nhất 2 vai trò | **Đạt** |
| | **K4.2** | Gõ trực tiếp URL trang quản trị bị chặn | **Đạt** |
| | **K4.3** | Xuất báo cáo (Excel/PDF) từ dữ liệu thật hoặc AI | **Thiếu** |
| **5. Hồ sơ & Vấn đáp (15%)** | **K5.1** | Sơ đồ PlantUML (Use Case, Class, Sequence) khớp code | **Thiếu** |
| | **K5.2** | Vấn đáp: chỉ đúng dòng cài luật & Dependency Rule | **Chưa xác minh** |
| | **K5.3** | Lịch sử commit rải đều trên ít nhất 3 tuần | **Thiếu** |

---

## III. DANH SÁCH CÔNG VIỆC CẦN HOÀN THIỆN THEO MỨC ƯU TIÊN

### Ưu tiên cao (Các mục đang bị điểm 0 cần khắc phục ngay):
1. **[K4.3] Tính năng Xuất Excel báo cáo tuổi nợ**: Tích hợp thư viện `ClosedXML`, viết UseCase xuất file Excel từ dữ liệu tuổi nợ thực tế để lấy trọn vẹn điểm tiêu chí tính năng nâng cao.
2. **[K5.1] Bổ sung 3 sơ đồ PlantUML**: Soạn thảo các file `.puml` lưu tại `docs/uml/` thể hiện Use Case, Class diagram và Sequence diagram luồng Lập hóa đơn & Phân bổ FIFO.

### Ưu tiên trung bình (Các mục Đạt một phần cần chuẩn hóa):
3. **[K2.1] Bổ sung trường "Số ngày nợ" cho Khách hàng**: Thêm cột `SoNgayNo` vào bảng `KhachHangs` và entity `KhachHang.cs` theo đặc tả Đề số 18.
4. **Hoàn thiện `README.md`**: Bổ sung hướng dẫn chi tiết cách cấu hình chuỗi kết nối, tài khoản mẫu và cách chạy ứng dụng từ Visual Studio / CLI.
