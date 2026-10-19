# BẢNG ĐÁNH GIÁ VÀ BẰNG CHỨNG ĐỒ ÁN CAPSTONE (GRADING.MD)

- **Môn học**: Lập trình Ứng dụng Web / Đồ án Capstone (ECO2415)
- **Đề tài**: Đề số 18 – Bán hàng & Công nợ Khách hàng (Sales & Accounts Receivable)
- **Nền tảng**: .NET 8 / C#
- **Mô hình Blazor**: Classic Blazor Server (sử dụng `AddServerSideBlazor`, `MapBlazorHub` và `_Host.cshtml`).
  > **Ghi chú đối chiếu công nghệ**: PDF Đề tài Mục 2 ghi nhận mô hình *Blazor Web App, render mode Interactive Server (tương đương Blazor Server trong video)*. Dự án hiện tại đang triển khai theo kiến trúc Classic Blazor Server tương đương video bài giảng để bảo toàn tính ổn định của pipeline Cookie Authentication. Chưa chuyển sang template root mới `AddRazorComponents` của .NET 8, do đó trạng thái mô hình công nghệ là **chưa chốt / cần giảng viên xác nhận**, không tự ý khẳng định là đã đáp ứng hoàn toàn yêu cầu công nghệ của đề cương.
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
  - **Trường còn thiếu / chưa chốt theo Đề 18**: Đặc tả ghi `Customer (hạn mức tín dụng, số ngày nợ)`. Ở đây cần phân biệt:
    - **Thời hạn tín dụng khách hàng (Số ngày nợ theo chính sách - Credit Terms)**: Quy định số ngày tối đa cho phép khách hàng được nợ kể từ ngày mua hàng trước khi bị coi là quá hạn (ví dụ 30 ngày). Bảng `KhachHangs` và entity `KhachHang.cs` hiện **chưa có trường này**.
    - **Số ngày quá hạn của từng hóa đơn**: Là chỉ số động được tính toán tại thời điểm chốt báo cáo cho từng khoản nợ (`SoNgayQuaHan = max(0, NgayBaoCao - HanThanhToan)`).
    - Hai khái niệm này hoàn toàn độc lập và không được nhầm lẫn.
- **Phần còn thiếu & Công việc cần thực hiện**:
  - Bổ sung cột `SoNgayNo INT DEFAULT (30)` (thời hạn tín dụng khách hàng) vào bảng `KhachHangs` và entity `KhachHang.cs`.
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
       - Bọc form trong `<EditForm EditContext="@editContextHoaDon" OnValidSubmit="LuuHoaDonAsync" novalidate>` (tắt popup native HTML tiếng Anh để validation Blazor thể hiện hoàn chỉnh). Triển khai `@implements IDisposable`.
       - Xóa dòng giải thích dưới Mã hóa đơn (*"Mã xem trước tự động cấp bởi hệ thống khi lưu."*), chỉ giữ nhãn và ô mã readonly theo đúng yêu cầu đã chốt.
       - Giữ nguyên hành vi disabled gốc của nút Lưu: `disabled="@(isSubmitting || VuotHanMucHienTai)"`.
       - Model nhập liệu: `public class FormTaoHoaDonModel`: `KhachHangId` [Range(1, int.MaxValue)], `NgayLap` [Required], `HanThanhToan` [Required] (kiểu `DateTime?`, bắt buộc chọn, không mặc định), `GhiChu` [StringLength(500)].
       - Input HTML thường: `NgayLap` `@bind:after="OnNgayLapChanged"`, `HanThanhToan` `@bind:after="OnHanThanhToanChanged"`, `GhiChu` `@bind:after="OnGhiChuChanged"`. Tất cả đều kích hoạt `NotifyFieldChanged` và đồng bộ nháp Scoped.
       - Kiểm tra số lượng dòng hàng lồng nhau: Đổi ô số lượng sang `type="text" inputmode="numeric"`, sử dụng `KiemTraSoLuongDongHang` chuẩn hóa dấu chấm/phẩy, phát hiện và hiển thị lỗi tiếng Việt cạnh ô nhập cho trường hợp số lượng $\le 0$ ("Số lượng phải lớn hơn 0.") hoặc không phải số nguyên ("Số lượng phải là số nguyên."). Khi nhập sai, không âm thầm giữ giá trị cũ mà cập nhật lỗi tức thì; `TongTienHoaDonMoi` chỉ tính các dòng hợp lệ; nút Lưu và `LuuHoaDonAsync` chặn tuyệt đối nếu còn dòng lỗi.
       - Hiển thị lỗi cạnh ô nhập: `<ValidationMessage For="..." />` cho `KhachHangId`, `NgayLap`, `HanThanhToan`, `GhiChu`, lỗi tiếng Việt dưới từng ô số lượng chi tiết, kèm cảnh báo dòng hàng `loiDanhSachSanPham` trực tiếp dưới bảng hàng hóa.
       - Tích hợp `ValidationMessageStore`, hủy đăng ký `hoaDonFieldChangedHandler` khi khởi tạo lại và trong `Dispose`.
    4. **Form Tạo Phiếu Thu Tiền & Phân bổ FIFO** (`DuAn_BanHangVaCongNoKhachHang/Pages/TaoThanhToanPage.razor`):
       - Bọc form trong `<EditForm EditContext="@editContextThanhToan" OnValidSubmit="LuuThanhToanAsync" novalidate>` (tắt popup native HTML tiếng Anh). Triển khai `@implements IDisposable`.
       - Giữ nguyên hành vi disabled gốc của nút Lưu: `disabled="@(isSubmitting || formModel.KhachHangId <= 0 || formModel.SoTien <= 0)"`.
       - Model nhập liệu: `public class FormTaoThanhToanModel`: `KhachHangId` [Range(1, int.MaxValue)], `NgayThanhToan` [Required], `PhuongThuc` [Required, StringLength(50)], `SoTien` `[SoTienThanhToan]`, `LoiDinhDangSoTien` (string?), `GhiChu` [StringLength(500)].
       - Phân định rõ ràng các loại lỗi qua `KiemTraSoTienThanhToan` và `[SoTienThanhToanAttribute]`:
         - Khi nhập chuỗi/ký tự không hợp lệ (như `abc`, `10a`): `formModel.SoTien = 0` (tuyệt đối không giữ giá trị cũ), `LoiDinhDangSoTien = "Số tiền thanh toán không hợp lệ."`. Thuộc tính `[SoTienThanhToan]` ưu tiên trả về **duy nhất một thông báo** *"Số tiền thanh toán không hợp lệ."*, không đồng thời phát sinh lỗi *"Số tiền thanh toán phải lớn hơn 0."*.
         - Khi nhập số $\le 0$ (như `0`, `-50`): Báo lỗi duy nhất *"Số tiền thanh toán phải lớn hơn 0."*.
         - Khi nhập số quá 2 chữ số thập phân (như `100.125`, `100,125`): Báo lỗi duy nhất *"Số tiền thanh toán chỉ được phép có tối đa 2 chữ số thập phân."*.
         - Khi sửa thành giá trị hợp lệ: Xóa toàn bộ lỗi cũ ngay lập tức, làm mới bản xem trước phân bổ FIFO.
       - Giữ nguyên quy tắc kiểm tra bảo vệ kép ở tầng UseCase/Repository, nút Lưu disabled và thuật toán FIFO.
       - Input HTML thường: Gắn `@bind:after` cho `NgayThanhToan`, `PhuongThuc`, `GhiChu` kích hoạt `NotifyFieldChanged`.
       - Hiển thị lỗi cạnh ô nhập: `<ValidationMessage For="..." />` cho từng trường.
       - Tích hợp `ValidationMessageStore` ánh xạ lỗi UseCase máy chủ vào đúng trường và hủy đăng ký event handler an toàn.
- **Bằng chứng kiểm thử / Demo**:
  - `BanHang.Tests/Components/FormValidationTests.cs`: 35 test cases tự động kiểm tra DataAnnotations trên 4 model form (`FormKhachHangModel`, `FormSanPhamModel`, `FormTaoHoaDonModel`, `FormTaoThanhToanModel`).
  - **Đối chiếu chênh lệch số lượng kiểm thử qua các lần rà soát**:
    1. *Giai đoạn K3.1*: 230 tests.
    2. *Đợt 1 K3.2 (Thêm FormValidationTests ban đầu)*: +18 tests (6 Khách hàng, 6 Sản phẩm, 3 Hóa đơn, 3 Phiếu thu) $\to$ **248 tests**.
    3. *Đợt 2 K3.2 (Bổ sung kiểm thử dòng hàng & số tiền)*: +12 test cases $\to$ **260 tests**, gồm chính xác:
       - `FormTaoThanhToanModel_SoTienAm_BaoLoiLonHonKhong` (+1)
       - `FormTaoThanhToanModel_SoTienQua2ChuSoThapPhan_BaoLoiToiDa2ChuSoThapPhan` (+1)
       - `TaoHoaDonPage_KiemTraSoLuong_SoThapPhan_BaoLoiSoNguyen` (+3: "1.5", "2,5", "0.1")
       - `TaoHoaDonPage_KiemTraSoLuong_SoKhongVaAm_BaoLoiLonHonKhong` (+3: "0", "-1", "-10")
       - `TaoHoaDonPage_KiemTraSoLuong_KyTuChu_BaoLoiSoNguyen` (+1: "abc")
       - `TaoHoaDonPage_KiemTraSoLuong_SoNguyenDuong_HopLe` (+3: "1", "50", "1000")
    4. *Đợt 3 K3.2 (Bổ sung kiểm thử đơn lỗi định dạng & helper KiemTraSoTienThanhToan)*: +9 test cases $\to$ **269 tests**, gồm chính xác:
       - `FormTaoThanhToanModel_LoiDinhDangSoTien_ChiBaoLoiKhongHopLe_KhongBaoLoiLonHonKhong` (+1)
       - `TaoThanhToanPage_KiemTraSoTienThanhToan_CacTruongHop` (+8: "abc", "10a", "0", "-50", "100.125", "100,125", "50000", "50000.5")
  - Kết quả chạy kiểm thử toàn solution: **269/269 tests Passed 100%** (bao gồm 234 test nghiệp vụ/tích hợp CSDL + 35 test form validation). Toàn bộ integration test trên DB test `QuanLyBanHangCongNo_Test` (khóa `sp_getapplock` timeout, cấn trừ FIFO, hạn mức tín dụng, trừ tồn kho...) đều vượt qua.
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
- **Trạng thái**: **Đạt**
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Mã nguồn sử dụng hệ thống lưới Bootstrap 5 (`col-md-4`, `col-md-6`, `col-12`, `d-flex flex-wrap`, `table-responsive`, thẻ `meta viewport`).
  - Thiết lập chuỗi Flexbox & Scrollable xuyên suốt (`modal-dialog` $\rightarrow$ `modal-content` $\rightarrow$ `form.modal-form-scrollable` $\rightarrow$ `modal-body`) tại `DuAn_BanHangVaCongNoKhachHang/wwwroot/css/site.css` (Dòng 121–195), `Pages/SanPhamPage.razor` (Dòng 137, 210, 214) và `Pages/KhachHangPage.razor` (Dòng 139), khắc phục triệt để lỗi đứt gãy flex do thẻ `<form>` do `EditForm` sinh ra.
  - **Bằng chứng kiểm tra thực tế**: Người dùng đã kiểm tra thủ công trên Microsoft Edge DevTools ở kích thước 390 × 844 px (chế độ mô phỏng iPhone 12/13/14 Pro) vào ngày 19/10/2026:
    1. **Menu điều hướng**: Thu gọn dạng hamburger/offcanvas, các liên kết chuyển trang hoạt động mượt mà.
    2. **Bảng và Chi tiết Hóa đơn**: Bảng danh sách cuộn ngang êm ái qua `table-responsive`, trang chi tiết hóa đơn `/hoa-don/{id}` hiển thị đầy đủ thông tin khách hàng, trạng thái thanh toán và danh sách chi tiết mặt hàng vừa vặn.
    3. **Modal Thêm/Sửa Khách hàng & Sản phẩm**: Lỗi cuộn modal đã được sửa hoàn tất; modal nằm gọn trong viewport chừa lề trên/dưới (`max-height: calc(100vh - 1rem)` / `100dvh`), header và footer chứa nút "Hủy bỏ" / "Thêm mới" / "Lưu thay đổi" cố định luôn nhìn thấy, vùng `modal-body` cuộn dọc mượt mà bằng chuột và thao tác vuốt cả khi có thông báo validation hoặc ảnh xem trước. Người dùng kiểm tra lại đạt 100%.
    4. **Form Phiếu thu & Phân bổ FIFO**: Trang `/thanh-toan/tao` hiển thị rõ các ô nhập, bảng xem trước phân bổ cấn trừ nợ FIFO cuộn xem dễ dàng.
    5. **Báo cáo tuổi nợ**: Trang `/bao-cao-tuoi-no` hiển thị tốt bộ lọc ngày chốt, các thẻ thống kê tổng hợp và bảng 4 nhóm tuổi nợ cuộn ngang mượt mà, nút xuất Excel hoạt động chuẩn.
    6. **Form Lập hóa đơn**: Trang `/hoa-don/tao` hiển thị gọn gàng thông tin khách hàng, ngày tháng, bảng danh mục hàng hóa và tổng tiền dự kiến.
    7. **Đăng nhập & Nút hiện/ẩn mật khẩu**: Trang `/Login` hiển thị vừa vặn, form đăng nhập, nút chuyển đổi ẩn/hiện mật khẩu và thông báo lỗi đăng nhập rõ ràng.
- **Giới hạn kiểm chứng**: Bằng chứng trên được xác nhận thông qua kiểm tra giả lập viewport (DevTools mô phỏng thiết bị di động 390 × 844 px), chưa phải kiểm tra trên điện thoại vật lý.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Tiêu chí K3.4 đã được người dùng kiểm tra lại và xác nhận Đạt.

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
- **Trạng thái**: **Đạt (Phần kết xuất và nội dung file .xlsx đã kiểm thử tự động; phần tải file trên trình duyệt thực tế chờ người dùng xác nhận)**
    - **Nhóm bản Báo Cáo Thuyết Minh A4 (Chính văn đồ án - Cỡ chữ in thực tế ĐẠT $\ge$ 9.0 pt)**:
      1. `docs/uml/BaoCao_ClassDiagram_KienTrucTongQuan.puml` & `.svg` (1104 x 851 px): Clean Architecture chuẩn: Presentation $\rightarrow$ UseCases $\rightarrow$ CoreBusiness; Plugins $\rightarrow$ UseCases (DIP). CoreBusiness độc lập 100%, không phụ thuộc Plugins. Cỡ chữ in trên A4 Landscape đạt **9.4 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
      2. `docs/uml/BaoCao_UseCase.puml` & `.svg` (639 x 713 px): 2 Actor (`NhanVienBanHang`, `KeToanCongNo`), phân quyền rõ ràng, bố cục cân đối trang dọc. Cỡ chữ in đạt **15.5 pt** (ĐẠT $\ge$ 9pt, rất to rõ).
      3. `docs/uml/BaoCao_DomainModel.puml` & `.svg` (626 x 1009 px): 5 Entity cốt lõi, Header-Line (1-n), quan hệ `ThanhToan 1 *-- 0..* PhanBoThanhToan` (chuẩn hóa nghiệp vụ phiếu thu chưa phân bổ khi khách nộp tiền thừa), phương thức kiểm tra hạn mức, cột tính toán PERSISTED. Cỡ chữ in đạt **10.3 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
      4. `docs/uml/BaoCao_Sequence_TaoHoaDon.puml` & `.svg` (820 x 1421 px): Tập trung luồng chính: Lifeline gộp `Xử lý nghiệp vụ (UseCase + Repository)`, mở Tx tại Repository, khóa UPDLOCK $\rightarrow$ gọi Luật 1 hạn mức trong transaction sau khi khóa khách hàng và đọc công nợ thực tế $\rightarrow$ cấp mã HDxxx $\rightarrow$ trừ tồn atomic $\rightarrow$ Commit. Cỡ chữ in đạt **9.3 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
      5. `docs/uml/BaoCao_Sequence_ThanhToanFIFO.puml` & `.svg` (919 x 1675 px): Tập trung luồng chính: Lifeline gộp `Xử lý nghiệp vụ (UseCase + Repository)`, Xem trước FIFO $\rightarrow$ Bấm lưu $\rightarrow$ Mở Tx tại Repo $\rightarrow$ Cấp mã PTxxx $\rightarrow$ Khóa UPDLOCK $\rightarrow$ Chạy lại FIFO & Đối chiếu snapshot $\rightarrow$ Ghi nhận Header/Line (0..* dòng phân bổ) $\rightarrow$ Commit. Cỡ chữ in đạt **10.2 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
    - **Nhóm bản Phân rã Chi tiết (Phụ lục Báo cáo - Ghi nhận trung thực: CHƯA ĐẠT 9.0 pt do mật độ chi tiết cao)**:
      - *Luồng Lập Hóa Đơn Bán Chịu*:
        - P1: `docs/uml/Sequence_TaoHoaDon_Phan1_KiemTra.puml` & `.svg`: Bước `[01]`–`[14]`, chữ in **~4.4 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P2: `docs/uml/Sequence_TaoHoaDon_Phan2_KhoaDuLieu.puml` & `.svg`: Bước `[15]`–`[38]`, chữ in **~3.6 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P3: `docs/uml/Sequence_TaoHoaDon_Phan3_CapMaCommit.puml` & `.svg`: Bước `[39]`–`[69]`, chữ in **~3.4 pt** (Chưa đạt 9pt, tra cứu phụ lục).
      - *Luồng Thu Tiền & Phân Bổ FIFO*:
        - P1: `docs/uml/Sequence_ThanhToanFIFO_Phan1_XemTruoc.puml` & `.svg`: Bước `[01]`–`[18]`, chữ in **~3.7 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P2: `docs/uml/Sequence_ThanhToanFIFO_Phan2_KhoaDoiSoat.puml` & `.svg`: Bước `[19]`–`[44]`, chữ in **~3.1 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P3: `docs/uml/Sequence_ThanhToanFIFO_Phan3_GhiNhanCommit.puml` & `.svg`: Bước `[45]`–`[68]`, chữ in **~4.2 pt** (Chưa đạt 9pt, tra cứu phụ lục).
    - **Nhóm bản Tham khảo Toàn cảnh (Full Reference)**:
      - `docs/uml/ClassDiagram.puml` & `.svg` (4488 x 1348 px): Toàn bộ chi tiết 4 tầng. Chữ in nếu ép A4: **~2.0 pt** (Chưa đạt 9pt).
      - `docs/uml/DomainModel.puml` & `.svg` (1502 x 1015 px): Toàn bộ thuộc tính & ghi chú SQL. Chữ in nếu ép A4: **~5.5 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_TaoHoaDon.puml` & `.svg` (2732 x 3545 px): Toàn bộ luồng gồm cả 6 nhánh break rollback. Chữ in: **~1.8 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_ThanhToanFIFO.puml` & `.svg` (2863 x 3084 px): Toàn bộ luồng gồm cả xem trước và lưu kèm mọi nhánh lỗi. Chữ in: **~1.9 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_TaoHoaDon_Phan2_Transaction.puml` & `.svg` (2294 x 2770 px): Bản 2 phần cũ ([15]–[69]), chữ in: **~2.4 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_ThanhToanFIFO_Phan2_Transaction.puml` & `.svg` (2594 x 2588 px): Bản 2 phần cũ ([19]–[68]), chữ in: **~2.15 pt** (Chưa đạt 9pt).
    - **Tài liệu PDF chính thức phục vụ báo cáo và kiểm chứng**:
      - `docs/uml/BaoCao_UML_A4.pdf`: Bản PDF 5 trang dành cho **chính văn báo cáo thuyết minh** đồ án. Đã loại bỏ khung kiểm chứng nét đứt và nhãn kiểm thử, bố trí sẵn lề an toàn 20mm, chữ đạt chuẩn từ 9.3 pt đến 15.5 pt.
      - `docs/uml/KiemChung_BoCuc_A4.pdf`: Tài liệu kiểm chứng 13 trang A4 tiêu chuẩn (vùng in có lề 20 mm kẻ nét đứt). Minh chứng tỷ lệ co thực tế, bảng đối soát kích thước chữ và kết luận ĐẠT / CHƯA ĐẠT cho từng sơ đồ một cách khách quan, trung thực (dòng thông số chân trang 2 dòng ngắt dòng chuẩn xác không bị cắt lề).
    - Tên file: `BaoCaoTuoiNo_{yyyyMMdd}.xlsx` chứa ngày chốt số liệu, không lộ thông tin nhạy cảm.
- **Bằng chứng kiểm thử / Demo**:
  - `BanHang.Tests/BaoCaoTuoiNoExcelTests.cs`: 7 test cases tự động:
    1. `Test01_ClosedXmlExporter_XuatFile_DocLaiDungCauTrucVaGiaTriSo`: Đọc lại file .xlsx sinh ra bằng ClosedXML, kiểm tra ngày chốt, freeze panes, STT, tên text, hạn mức/tuổi nợ/tổng nợ là số `decimal`, kiểm tra công thức injection không bị thực thi (`HasFormula = false`), khớp hàng tổng và tiền chưa phân bổ.
    2. `Test02_ClosedXmlExporter_DuLieuTrong_TaoFileHopLe`: Kiểm tra khi danh sách rỗng vẫn tạo file Excel hợp lệ, có thông báo không có dữ liệu, hàng tổng = 0 đ.
    3. `Test03_XuatExcelUseCase_ChuaDangNhap_BiTuChoi`: Chặn người dùng nặc danh (ném `UnauthorizedAccessException`).
    4. `Test04_XuatExcelUseCase_SaiVaiTro_NhanVienBanHang_BiTuChoi`: Chặn vai trò Nhân viên bán hàng (ném `UnauthorizedAccessException`).
    5. `Test05_XuatExcelUseCase_KeToanCongNo_XuatThanhCong`: Xác thực Kế toán công nợ xuất file thành công, đúng tên file theo ngày chốt.
    6. `Test06_XuatExcelUseCase_DuLieuThayDoiSoVoiSnapshot_PhatHienVaTuChoi`: Phát hiện dữ liệu DB thay đổi so với snapshot đang xem trên màn hình, chặn xuất và trả thông báo tiếng Việt rõ ràng.
    7. `Test07_XuatExcelUseCase_NgayNhapChuaApDung_KhongAnhHuongNgayXuat`: Khẳng định việc thay đổi ngày nhập liệu mà chưa áp dụng sẽ không làm sai lệch ngày chốt và dữ liệu của file xuất ra.
  - Kết quả chạy kiểm thử toàn solution: **276/276 tests Passed 100%**.
  - *Phân biệt trạng thái*: Việc tạo file, cấu trúc dữ liệu, kiểm tra quyền và phát hiện thay đổi đã được kiểm thử tự động 100%; thao tác tải tệp trực tiếp trên trình duyệt thực tế chờ người dùng nhấn nút "Xuất Excel" để trải nghiệm.
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Tiêu chí K4.3 đã hoàn thành trọn vẹn theo đúng yêu cầu đề bài.

---

### Nhóm 5: Hồ sơ Thiết kế (UML) & Thuyết trình Vấn đáp (15%)

#### K5.1: Sơ đồ thiết kế PlantUML (Use Case, Class, Sequence) khớp với mã nguồn
- **Yêu cầu của thầy**: Use Case, Class, Sequence (PlantUML) khớp tên lớp và luồng trong code.
- **Trạng thái**: **Đạt** (Đã kiểm chứng render cục bộ 19/19 tệp SVG, 19 tệp PDF vector và tài liệu kiểm chứng bố cục A4 thực tế `KiemChung_BoCuc_A4.pdf` gồm 13 trang, không phụ thuộc dịch vụ ngoài)
- **Đường dẫn tương đối & Số dòng làm bằng chứng**:
  - Thư mục hồ sơ thiết kế: `docs/uml/` chứa đầy đủ mã nguồn PlantUML chuẩn, tệp ảnh vector SVG và PDF vector tương ứng:
    - **Nhóm bản Báo Cáo Thuyết Minh A4 (Chính văn đồ án - Cỡ chữ in thực tế ĐẠT $\ge$ 9.0 pt)**:
      1. `docs/uml/BaoCao_ClassDiagram_KienTrucTongQuan.puml` & `.svg` (1104 x 851 px): Clean Architecture chuẩn: Presentation $\rightarrow$ UseCases $\rightarrow$ CoreBusiness; Plugins $\rightarrow$ UseCases (DIP). CoreBusiness độc lập 100%, không phụ thuộc Plugins. Cỡ chữ in trên A4 Landscape đạt **9.4 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
      2. `docs/uml/BaoCao_UseCase.puml` & `.svg` (639 x 713 px): 2 Actor (`NhanVienBanHang`, `KeToanCongNo`), phân quyền rõ ràng, bố cục cân đối trang dọc. Cỡ chữ in đạt **15.5 pt** (ĐẠT $\ge$ 9pt, rất to rõ).
      3. `docs/uml/BaoCao_DomainModel.puml` & `.svg` (626 x 1009 px): 5 Entity cốt lõi, Header-Line (1-n), quan hệ `ThanhToan 1 *-- 0..* PhanBoThanhToan` (chuẩn hóa nghiệp vụ phiếu thu chưa phân bổ khi khách nộp tiền thừa), phương thức kiểm tra hạn mức, cột tính toán PERSISTED. Cỡ chữ in đạt **10.3 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
      4. `docs/uml/BaoCao_Sequence_TaoHoaDon.puml` & `.svg` (820 x 1421 px): Tập trung luồng chính: Lifeline gộp `Xử lý nghiệp vụ (UseCase + Repository)`, mở Tx tại Repository, khóa UPDLOCK $\rightarrow$ gọi Luật 1 hạn mức trong transaction sau khi khóa khách hàng và đọc công nợ thực tế $\rightarrow$ cấp mã HDxxx $\rightarrow$ trừ tồn atomic $\rightarrow$ Commit. Cỡ chữ in đạt **9.3 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
      5. `docs/uml/BaoCao_Sequence_ThanhToanFIFO.puml` & `.svg` (919 x 1675 px): Tập trung luồng chính: Lifeline gộp `Xử lý nghiệp vụ (UseCase + Repository)`, Xem trước FIFO $\rightarrow$ Bấm lưu $\rightarrow$ Mở Tx tại Repo $\rightarrow$ Cấp mã PTxxx $\rightarrow$ Khóa UPDLOCK $\rightarrow$ Chạy lại FIFO & Đối chiếu snapshot $\rightarrow$ Ghi nhận Header/Line (0..* dòng phân bổ) $\rightarrow$ Commit. Cỡ chữ in đạt **10.2 pt** (ĐẠT $\ge$ 9pt, dễ đọc).
    - **Nhóm bản Phân rã Chi tiết (Phụ lục Báo cáo - Ghi nhận trung thực: CHƯA ĐẠT 9.0 pt do mật độ chi tiết cao)**:
      - *Luồng Lập Hóa Đơn Bán Chịu*:
        - P1: `docs/uml/Sequence_TaoHoaDon_Phan1_KiemTra.puml` & `.svg`: Bước `[01]`–`[14]`, chữ in **~4.4 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P2: `docs/uml/Sequence_TaoHoaDon_Phan2_KhoaDuLieu.puml` & `.svg`: Bước `[15]`–`[38]`, chữ in **~3.6 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P3: `docs/uml/Sequence_TaoHoaDon_Phan3_CapMaCommit.puml` & `.svg`: Bước `[39]`–`[69]`, chữ in **~3.4 pt** (Chưa đạt 9pt, tra cứu phụ lục).
      - *Luồng Thu Tiền & Phân Bổ FIFO*:
        - P1: `docs/uml/Sequence_ThanhToanFIFO_Phan1_XemTruoc.puml` & `.svg`: Bước `[01]`–`[18]`, chữ in **~3.7 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P2: `docs/uml/Sequence_ThanhToanFIFO_Phan2_KhoaDoiSoat.puml` & `.svg`: Bước `[19]`–`[44]`, chữ in **~3.1 pt** (Chưa đạt 9pt, tra cứu phụ lục).
        - P3: `docs/uml/Sequence_ThanhToanFIFO_Phan3_GhiNhanCommit.puml` & `.svg`: Bước `[45]`–`[68]`, chữ in **~4.2 pt** (Chưa đạt 9pt, tra cứu phụ lục).
    - **Nhóm bản Tham khảo Toàn cảnh (Full Reference)**:
      - `docs/uml/ClassDiagram.puml` & `.svg` (4488 x 1348 px): Toàn bộ chi tiết 4 tầng. Chữ in nếu ép A4: **~2.0 pt** (Chưa đạt 9pt).
      - `docs/uml/DomainModel.puml` & `.svg` (1502 x 1015 px): Toàn bộ thuộc tính & ghi chú SQL. Chữ in nếu ép A4: **~5.5 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_TaoHoaDon.puml` & `.svg` (2732 x 3545 px): Toàn bộ luồng gồm cả 6 nhánh break rollback. Chữ in: **~1.8 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_ThanhToanFIFO.puml` & `.svg` (2863 x 3084 px): Toàn bộ luồng gồm cả xem trước và lưu kèm mọi nhánh lỗi. Chữ in: **~1.9 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_TaoHoaDon_Phan2_Transaction.puml` & `.svg` (2294 x 2770 px): Bản 2 phần cũ ([15]–[69]), chữ in: **~2.4 pt** (Chưa đạt 9pt).
      - `docs/uml/Sequence_ThanhToanFIFO_Phan2_Transaction.puml` & `.svg` (2594 x 2588 px): Bản 2 phần cũ ([19]–[68]), chữ in: **~2.15 pt** (Chưa đạt 9pt).
    - **Tài liệu PDF chính thức phục vụ báo cáo và kiểm chứng**:
      - `docs/uml/BaoCao_UML_A4.pdf`: Bản PDF 5 trang dành cho **chính văn báo cáo thuyết minh** đồ án. Đã loại bỏ khung kiểm chứng nét đứt và nhãn kiểm thử, bố trí sẵn lề an toàn 20mm, chữ đạt chuẩn từ 9.3 pt đến 15.5 pt.
      - `docs/uml/KiemChung_BoCuc_A4.pdf`: Tài liệu kiểm chứng 13 trang A4 tiêu chuẩn (vùng in có lề 20 mm kẻ nét đứt). Minh chứng tỷ lệ co thực tế, bảng đối soát kích thước chữ và kết luận ĐẠT / CHƯA ĐẠT cho từng sơ đồ một cách khách quan, trung thực (dòng thông số chân trang 2 dòng ngắt dòng chuẩn xác không bị cắt lề).
- **Bằng chứng kiểm thử / Demo**:
  - **Bộ công cụ portable**: `Eclipse Temurin OpenJDK 21.0.12.1+1-jre` (x64 portable) + `PlantUML 1.2026.9` (tại `C:\Users\Admin\.tools\plantuml\`).
  - **Lệnh thực thi**:
    `java.exe -jar plantuml.jar -tsvg -charset UTF-8 docs/uml/*.puml`
    `java.exe -jar plantuml.jar -tpdf -charset UTF-8 docs/uml/*.puml`
  - **Kết quả render**: 19/19 tệp `.svg` và 19/19 tệp `.pdf` hợp lệ 100%, không phát sinh lỗi cú pháp (`HasSyntaxError = False`), hỗ trợ tiếng Việt Unicode hoàn hảo.
- **Lưu ý đánh giá**: Sơ đồ thiết kế đã tạo và kiểm chứng render đạt không đồng nghĩa tiêu chí K5.2 vấn đáp đã đạt (tiêu chí K5.2 phụ thuộc vào phần trả lời trực tiếp của từng thành viên trong buổi bảo vệ).
- **Phần còn thiếu & Công việc cần thực hiện**: Không có. Tiêu chí K5.1 đã hoàn thành trọn vẹn cả mã nguồn PlantUML, file SVG, PDF vector và tài liệu kiểm chứng bố cục A4 thực tế.

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
- **Đạt**: **15 tiêu chí** (`K1.1`, `K1.2`, `K1.3`, `K1.4`, `K2.2`, `K2.3`, `K2.4`, `K3.1`, `K3.2`, `K3.3`, `K3.4`, `K4.1`, `K4.2`, `K4.3`, `K5.1`)
- **Một phần**: **1 tiêu chí** (`K2.1`)
- **Thiếu (0 điểm)**: **1 tiêu chí** (`K5.3`)
- **Chưa xác minh**: **1 tiêu chí** (`K5.2`)

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
| | **K3.4** | Tương thích màn hình điện thoại (bề rộng 390px) | **Đạt** |
| **4. Bảo mật & Nâng cao (15%)** | **K4.1** | Đăng nhập, đăng xuất, ít nhất 2 vai trò | **Đạt** |
| | **K4.2** | Gõ trực tiếp URL trang quản trị bị chặn | **Đạt** |
| | **K4.3** | Xuất báo cáo (Excel/PDF) từ dữ liệu thật hoặc AI | **Đạt** (File .xlsx đã kiểm thử tự động, tải trình duyệt chờ xác nhận) |
| **5. Hồ sơ & Vấn đáp (15%)** | **K5.1** | Sơ đồ PlantUML (Use Case, Class, Sequence) khớp code | **Đạt** (Đã render cục bộ 19/19 tệp SVG, PDF vector & kiểm chứng PDF A4 lề 20mm 13 trang) |
| | **K5.2** | Vấn đáp: chỉ đúng dòng cài luật & Dependency Rule | **Chưa xác minh** |
| | **K5.3** | Lịch sử commit rải đều trên ít nhất 3 tuần | **Thiếu** |

---

## III. DANH SÁCH CÔNG VIỆC CẦN HOÀN THIỆN THEO MỨC ƯU TIÊN

### Ưu tiên cao:
*(Không còn mục điểm 0 nào về mã nguồn, tính năng hay hồ sơ thiết kế UML; tiêu chí K5.3 thuộc về lịch sử Git quá khứ, nhóm cam kết giữ trung thực không giả lập commit).*

### Ưu tiên trung bình (Các mục Đạt một phần cần chuẩn hóa):
1. **[K2.1] Bổ sung trường "Số ngày nợ" cho Khách hàng**: Thêm cột `SoNgayNo` vào bảng `KhachHangs` và entity `KhachHang.cs` theo đặc tả Đề số 18.
2. **Hoàn thiện `README.md`**: Bổ sung hướng dẫn chi tiết cách cấu hình chuỗi kết nối, tài khoản mẫu và cách chạy ứng dụng từ Visual Studio / CLI.
