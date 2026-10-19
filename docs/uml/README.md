# HỒ SƠ THIẾT KẾ PLANTUML - HỆ THỐNG BÁN HÀNG & CÔNG NỢ KHÁCH HÀNG (ĐỀ 18)

Tài liệu này cung cấp toàn bộ hồ sơ thiết kế phân tích hệ thống bằng ngôn ngữ mô hình hóa PlantUML (`.puml`) theo kiến trúc **Clean Architecture 4 tầng**, khớp với mã nguồn C# thực tế và đối chiếu với đặc tả tại `capstone-topics-2026(1).pdf` (Mục 7 Đề 18, Mục 11 & Mục 12 Tiêu chí K5.1).

> **Trạng thái kiểm chứng hồ sơ thiết kế:**
> **Đã hoàn tất kiểm chứng render trực quan cục bộ 100% (19/19 tệp SVG, 19 tệp PDF vector và tài liệu kiểm chứng bố cục A4 thực tế `KiemChung_BoCuc_A4.pdf` gồm 13 trang)** bằng bộ công cụ portable chính thức (Eclipse Temurin OpenJDK 21 + PlantUML 1.2026.9). Toàn bộ sơ đồ đều hợp lệ cú pháp (`HasSyntaxError = False`), hỗ trợ tiếng Việt Unicode UTF-8 chuẩn xác, không bị lỗi font hay cắt chồng và không gửi dữ liệu ra bên ngoài.

---

## I. PHÂN LOẠI DANH MỤC SƠ ĐỒ THIẾT KẾ THEO MỤC ĐÍCH SỬ DỤNG

Hồ sơ thiết kế được tổ chức thành **3 nhóm bản vẽ** rõ ràng nhằm phục vụ đúng ngữ cảnh sử dụng:

### 1. Nhóm Bản Báo Cáo Thuyết Minh A4 (Chính văn đồ án - Chữ in đạt $\ge$ 9.0 pt)
*Được thiết kế tinh giản nhãn, số đối tượng tham gia và tập trung luồng chính để bảo đảm **khi in hoặc xem ở tỷ lệ 100% trên trang A4 lề 20 mm, chữ luôn to rõ đạt từ 9.3 pt đến 15.5 pt**, người đọc và hội đồng chấm thi đọc dễ dàng không cần phóng to. Toàn bộ 5 sơ đồ báo cáo đã được đóng gói thành tệp PDF sạch [`BaoCao_UML_A4.pdf`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_UML_A4.pdf) (đã loại bỏ khung kiểm chứng nét đứt và nhãn kiểm thử, bố trí sẵn lề an toàn 20mm để chèn trực tiếp vào báo cáo).*

| STT | Tệp PlantUML | Tệp ảnh SVG | Tệp PDF Vector | Khổ A4 | Cỡ chữ xem 100% | Đánh giá kích thước chữ | Nội dung kiến trúc / Nghiệp vụ |
| :---: | :--- | :--- | :--- | :---: | :---: | :---: | :--- |
| **01** | [`BaoCao_ClassDiagram_KienTrucTongQuan.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_ClassDiagram_KienTrucTongQuan.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_ClassDiagram_KienTrucTongQuan.svg) | [PDF](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_ClassDiagram_KienTrucTongQuan.pdf) | Ngang | **9.4 pt** | **ĐẠT (>= 9pt, dễ đọc)** | **Clean Architecture chuẩn**: Thể hiện rõ ràng Presentation $\rightarrow$ UseCases $\rightarrow$ CoreBusiness; Plugins $\rightarrow$ UseCases (DIP). CoreBusiness hoàn toàn độc lập 100%, không phụ thuộc Plugins. |
| **02** | [`BaoCao_UseCase.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_UseCase.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_UseCase.svg) | [PDF](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_UseCase.pdf) | Dọc | **15.5 pt** | **ĐẠT (>= 9pt, rất to rõ)** | 2 Actor (`NhanVienBanHang`, `KeToanCongNo`), phân quyền rõ ràng, bố cục cân đối giữa trang dọc A4. |
| **03** | [`BaoCao_DomainModel.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_DomainModel.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_DomainModel.svg) | [PDF](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_DomainModel.pdf) | Dọc | **10.3 pt** | **ĐẠT (>= 9pt, dễ đọc)** | 5 Entity cốt lõi, Header-Line, quan hệ `ThanhToan 1 *-- 0..* PhanBoThanhToan` (chuẩn hóa cho trường hợp khách nộp tiền thừa chưa phân bổ), kiểm tra hạn mức, cột tính toán PERSISTED. |
| **04** | [`BaoCao_Sequence_TaoHoaDon.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_TaoHoaDon.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_TaoHoaDon.svg) | [PDF](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_TaoHoaDon.pdf) | Dọc | **9.3 pt** | **ĐẠT (>= 9pt, dễ đọc)** | **Tập trung luồng chính**: Lifeline gộp `Xử lý nghiệp vụ (UseCase + Repository)`, mở Tx tại Repository, khóa khách hàng & sản phẩm UPDLOCK $\rightarrow$ gọi Luật 1 hạn mức trong transaction sau khi đọc công nợ thực tế $\rightarrow$ cấp mã HDxxx $\rightarrow$ trừ tồn atomic $\rightarrow$ Commit. |
| **05** | [`BaoCao_Sequence_ThanhToanFIFO.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_ThanhToanFIFO.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_ThanhToanFIFO.svg) | [PDF](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_ThanhToanFIFO.pdf) | Dọc | **10.2 pt** | **ĐẠT (>= 9pt, dễ đọc)** | **Tập trung luồng chính**: Lifeline gộp `Xử lý nghiệp vụ (UseCase + Repository)`, Xem trước FIFO $\rightarrow$ Bấm lưu $\rightarrow$ Mở Tx tại Repo $\rightarrow$ Cấp mã PTxxx $\rightarrow$ Khóa UPDLOCK $\rightarrow$ Chạy lại FIFO & Đối chiếu snapshot $\rightarrow$ Ghi nhận Header/Line (0..* dòng phân bổ) $\rightarrow$ Commit. |

---

### 2. Nhóm Bản Phân Rã Chi Tiết (Phụ lục Báo cáo - Tra cứu kỹ thuật)
*Phân rã luồng tuần tự thành từng giai đoạn nghiệp vụ nhỏ hơn. Ghi nhận trung thực: Do mật độ chi tiết cao, nếu ép vào 1 trang A4 thì cỡ chữ chỉ đạt từ **3.1 pt đến 4.4 pt (CHƯA ĐẠT tiêu chuẩn dễ đọc $\ge$ 9.0 pt)**, phù hợp đặt ở phần Phụ lục hoặc xem trên màn hình lớn.*

- **Luồng Lập Hóa Đơn Bán Chịu**:
  - P1: [`Sequence_TaoHoaDon_Phan1_KiemTra.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_TaoHoaDon_Phan1_KiemTra.puml) (Bước `[01]`–`[14]`): Nhập liệu, lưu nháp circuit, validation UI. Cỡ chữ xem 100%: **~4.4 pt** *(Chưa đạt 9pt)*.
  - P2: [`Sequence_TaoHoaDon_Phan2_KhoaDuLieu.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_TaoHoaDon_Phan2_KhoaDuLieu.puml) (Bước `[15]`–`[38]`): Mở transaction, khóa UPDLOCK, kiểm tra Luật 1 hạn mức. Cỡ chữ xem 100%: **~3.6 pt** *(Chưa đạt 9pt)*.
  - P3: [`Sequence_TaoHoaDon_Phan3_CapMaCommit.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_TaoHoaDon_Phan3_CapMaCommit.puml) (Bước `[39]`–`[69]`): `sp_getapplock` cấp mã, Insert Header-Line, trừ tồn atomic, commit. Cỡ chữ xem 100%: **~3.4 pt** *(Chưa đạt 9pt)*.
- **Luồng Thu Tiền & Phân Bổ FIFO**:
  - P1: [`Sequence_ThanhToanFIFO_Phan1_XemTruoc.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_ThanhToanFIFO_Phan1_XemTruoc.puml) (Bước `[01]`–`[18]`): Xem trước, đọc hóa đơn nợ, chạy FIFO đối soát. Cỡ chữ xem 100%: **~3.7 pt** *(Chưa đạt 9pt)*.
  - P2: [`Sequence_ThanhToanFIFO_Phan2_KhoaDoiSoat.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_ThanhToanFIFO_Phan2_KhoaDoiSoat.puml) (Bước `[19]`–`[44]`): Bấm lưu, validation, phân quyền, `sp_getapplock`, khóa khách hàng & đọc lại. Cỡ chữ xem 100%: **~3.1 pt** *(Chưa đạt 9pt)*.
  - P3: [`Sequence_ThanhToanFIFO_Phan3_GhiNhanCommit.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_ThanhToanFIFO_Phan3_GhiNhanCommit.puml) (Bước `[45]`–`[68]`): Chạy lại FIFO có khóa, đối chiếu snapshot, ghi CSDL và commit. Cỡ chữ xem 100%: **~4.2 pt** *(Chưa đạt 9pt)*.

---

### 3. Nhóm Bản Tham Khảo Toàn Cảnh (Full Master Reference Diagrams)
*Dành cho việc tra cứu toàn diện trên màn hình lớn hoặc đối soát toàn bộ mã nguồn C# và các nhánh lỗi chi tiết.*

| STT | Tệp PlantUML | Tệp ảnh SVG | Kích thước (px) | Cỡ chữ nếu ép vào A4 | Mục đích sử dụng |
| :---: | :--- | :--- | :---: | :---: | :--- |
| M1 | [`ClassDiagram.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/ClassDiagram.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/ClassDiagram.svg) | 4488 x 1348 | ~2.0 pt (Chưa đạt) | Toàn bộ các class, interface, method signature đầy đủ cả 4 tầng. |
| M2 | [`DomainModel.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/DomainModel.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/DomainModel.svg) | 1502 x 1015 | ~5.5 pt (Chưa đạt) | Mô hình miền đầy đủ ghi chú SQL Server và các thuộc tính chi tiết. |
| M3 | [`Sequence_TaoHoaDon.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_TaoHoaDon.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_TaoHoaDon.svg) | 2732 x 3545 | ~1.8 pt (Chưa đạt) | Toàn cảnh luồng tạo hóa đơn gồm tất cả 6 nhánh break rollback chi tiết. |
| M4 | [`Sequence_ThanhToanFIFO.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_ThanhToanFIFO.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_ThanhToanFIFO.svg) | 2863 x 3084 | ~1.9 pt (Chưa đạt) | Toàn cảnh luồng thu tiền FIFO gồm cả xem trước và lưu kèm mọi nhánh lỗi. |
| M5 | [`Sequence_TaoHoaDon_Phan2_Transaction.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_TaoHoaDon_Phan2_Transaction.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_TaoHoaDon_Phan2_Transaction.svg) | 2294 x 2770 | ~2.4 pt (Chưa đạt) | Bản 2 phần cũ (Bước `[15]`–`[69]`). |
| M6 | [`Sequence_ThanhToanFIFO_Phan2_Transaction.puml`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_ThanhToanFIFO_Phan2_Transaction.puml) | [SVG](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/Sequence_ThanhToanFIFO_Phan2_Transaction.svg) | 2594 x 2588 | ~2.15 pt (Chưa đạt) | Bản 2 phần cũ (Bước `[19]`–`[68]`). |

---

## II. GIẢI THÍCH CHI TIẾT CÁC ĐIỂM KIỂM SOÁT LỖI & ROLLBACK

Trong bản báo cáo thuyết minh A4 ([`BaoCao_Sequence_TaoHoaDon`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_TaoHoaDon.puml) và [`BaoCao_Sequence_ThanhToanFIFO`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_Sequence_ThanhToanFIFO.puml)), sơ đồ tập trung vào luồng chính thành công (Happy Path) để giữ cỡ chữ $\ge$ 9.0 pt. Chi tiết các điểm kiểm soát lỗi và hủy giao dịch (Rollback) được chuẩn hóa theo bảng sau:

### 1. Luồng Lập Hóa Đơn Bán Chịu:
| Thứ tự kiểm tra | Điều kiện kích hoạt lỗi / vi phạm | Hành động xử lý trên CSDL | Phản hồi giao diện người dùng |
| :---: | :--- | :--- | :--- |
| **1. UI Validation** | Trường rỗng, số lượng $\le$ 0, trùng sản phẩm | Chưa mở Transaction CSDL | Hiển thị thông báo lỗi cạnh trường nhập liệu |
| **2. Phân quyền** | Người dùng không thuộc vai trò `NhanVienBanHang` | Ném `UnauthorizedAccessException` | Hiển thị giao diện Không có quyền truy cập |
| **3. Khách hàng** | Khách hàng không tồn tại hoặc `TrangThai == false` | `transaction.Rollback()` | Báo lỗi: Khách hàng hiện đang bị tạm khóa |
| **4. Sản phẩm & Giá** | Ngừng bán, đổi giá hiển thị, không đủ tồn kho | `transaction.Rollback()` | Báo lỗi: Đổi giá / Thiếu tồn / Ngừng kinh doanh |
| **5. Luật 1 Hạn mức** | $(CurrentDebt + NewInvoice) > CreditLimit$ | `transaction.Rollback()` | Báo lỗi: Tổng công nợ mới vượt quá hạn mức tín dụng |
| **6. Khóa cấp mã** | Timeout hoặc deadlock victim khi `sp_getapplock` | `transaction.Rollback()` | Thông báo: Hệ thống bận cấp mã, vui lòng thử lại |
| **7. Trừ tồn atomic** | Hụt tồn kho đồng thời phút chót (`rowsAffected != 1`) | `transaction.Rollback()` | Báo lỗi: Số lượng tồn kho không đủ đáp ứng |
| **8. Ngoại lệ kỹ thuật** | Bất kỳ `SqlException` hoặc `Exception` phát sinh | `transaction.Rollback()` | Báo lỗi sự cố kỹ thuật, giữ nguyên dữ liệu nháp |

### 2. Luồng Thu Tiền & Phân Bổ FIFO:
| Thứ tự kiểm tra | Điều kiện kích hoạt lỗi / vi phạm | Hành động xử lý trên CSDL | Phản hồi giao diện người dùng |
| :---: | :--- | :--- | :--- |
| **1. UI Validation** | Số tiền $\le$ 0, chưa chọn khách hàng | Chưa mở Transaction CSDL | Hiển thị lỗi cạnh ô nhập số tiền |
| **2. Phân quyền** | Người dùng không thuộc vai trò `KeToanCongNo` | Ném `UnauthorizedAccessException` | Chặn truy cập trang và ném ngoại lệ |
| **3. Khóa cấp mã** | Tranh chấp khóa độc quyền `sp_getapplock` ($< 0$) | `transaction.Rollback()` | Thông báo hệ thống bận, vui lòng thử lại |
| **4. Khách hàng** | Khách hàng không tồn tại trong CSDL | `transaction.Rollback()` | Báo lỗi khách hàng không tồn tại |
| **5. Đối chiếu snapshot** | Dữ liệu công nợ bị thay đổi so với bản xem trước | `transaction.Rollback()` | Cảnh báo: Dữ liệu công nợ đã thay đổi, cập nhật bảng phân bổ mới, yêu cầu xác nhận lại |
| **6. Ngoại lệ kỹ thuật** | Bất kỳ `SqlException` hoặc `Exception` phát sinh | `transaction.Rollback()` | Báo lỗi hệ thống, hủy toàn bộ giao dịch CSDL |

---

## III. BÁO CÁO CÔNG CỤ RENDER CỤC BỘ & KẾT QUẢ XUẤT ẢNH VECTOR

### 1. Báo cáo cấu hình công cụ portable cục bộ
- **Java Runtime**: `Eclipse Temurin OpenJDK 21.0.12.1+1-jre` (bản portable x64 chính thức).
- **PlantUML Engine**: `PlantUML version 1.2026.9` (phát hành chính thức 2026-10-17).
- **Vị trí lưu trữ**: `C:\Users\Admin\.tools\plantuml\` (thư mục công cụ riêng của người dùng, không can thiệp hệ thống hay project).
- **Nguyên tắc an toàn**: Toàn bộ quá trình biên dịch diễn ra **100% offline cục bộ trên máy tính**, tuyệt đối không gửi mã nguồn sơ đồ hay dữ liệu lên bất kỳ máy chủ render công khai nào.

### 2. Lệnh thực thi biên dịch
```powershell
# 1. Biên dịch toàn bộ sơ đồ sang vector SVG:
& "C:\Users\Admin\.tools\plantuml\jre\jdk-21.0.12.1+1-jre\bin\java.exe" `
    -jar "C:\Users\Admin\.tools\plantuml\plantuml.jar" `
    -tsvg -charset UTF-8 `
    "docs/uml/*.puml"

# 2. Biên dịch toàn bộ sơ đồ sang native vector PDF:
& "C:\Users\Admin\.tools\plantuml\jre\jdk-21.0.12.1+1-jre\bin\java.exe" `
    -jar "C:\Users\Admin\.tools\plantuml\plantuml.jar" `
    -tpdf -charset UTF-8 `
    "docs/uml/*.puml"
```

---

## IV. TÀI LIỆU KIỂM CHỨNG BỐ CỤC A4 & BẢN CHÈN BÁO CÁO CHÍNH THỨC

Hệ thống cung cấp 2 tệp tài liệu PDF phục vụ 2 mục đích riêng biệt:
1. **[`BaoCao_UML_A4.pdf`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/BaoCao_UML_A4.pdf)**: Bản PDF 5 trang dành cho **chính văn báo cáo thuyết minh** đồ án. Đã loại bỏ khung kiểm chứng nét đứt và nhãn kiểm thử, bố trí sẵn lề an toàn 20mm, chữ đạt chuẩn từ 9.3 pt đến 15.5 pt.
2. **[`KiemChung_BoCuc_A4.pdf`](file:///d:/LapTrinhUngDungWeb/DoAnCuoiKy/DuAn_BanHangVaCongNoKhachHang/docs/uml/KiemChung_BoCuc_A4.pdf)** *(hoặc `KiemChung_BoCuc_A4_Moi.pdf`)*: Bản PDF kiểm chứng kỹ thuật 13 trang (có khung lề in 20 mm nét đứt, dòng thông số chân trang 2 dòng ngắt dòng chuẩn xác không bị cắt lề).

### Bảng đối soát kích thước in và cỡ chữ thực tế trên từng trang:
- *Khổ in khả dụng*: Portrait: `481.9 x 728.5 pt` (170 x 257 mm) | Landscape: `728.5 x 481.9 pt` (257 x 170 mm).
- *Tiêu chuẩn kích thước chữ*: $\ge$ 9.0 pt (dễ đọc ở tỷ lệ hiển thị 100%).

| Trang | Tên sơ đồ | Nhóm sử dụng | Khổ A4 | Cỡ chữ xem 100% | Kết luận kích thước chữ |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **01** | Bìa Báo cáo & Bảng đối soát | Bìa | Dọc | 8.5–15 pt | Bìa thuyết minh, đầy đủ thông số |
| **02** | `BaoCao_ClassDiagram_KienTrucTongQuan` | **Báo cáo** | Ngang | **9.4 pt** | **ĐẠT (>= 9pt, dễ đọc)** |
| **03** | `BaoCao_UseCase` | **Báo cáo** | Dọc | **15.5 pt** | **ĐẠT (>= 9pt, rất to rõ)** |
| **04** | `BaoCao_DomainModel` | **Báo cáo** | Dọc | **10.3 pt** | **ĐẠT (>= 9pt, dễ đọc)** |
| **05** | `BaoCao_Sequence_TaoHoaDon` | **Báo cáo** | Dọc | **9.3 pt** | **ĐẠT (>= 9pt, dễ đọc)** |
| **06** | `BaoCao_Sequence_ThanhToanFIFO` | **Báo cáo** | Dọc | **10.2 pt** | **ĐẠT (>= 9pt, dễ đọc)** |
| **07** | Hóa đơn P1: Giao diện (`[01]–[14]`) | Phụ lục | Ngang | **4.4 pt** | **CHƯA ĐẠT 9pt (Chi tiết phụ lục)** |
| **08** | Hóa đơn P2: Khóa dữ liệu (`[15]–[38]`) | Phụ lục | Ngang | **3.6 pt** | **CHƯA ĐẠT 9pt (Chi tiết phụ lục)** |
| **09** | Hóa đơn P3: Cấp mã & Commit (`[39]–[69]`) | Phụ lục | Ngang | **3.4 pt** | **CHƯA ĐẠT 9pt (Chi tiết phụ lục)** |
| **10** | FIFO P1: Preview (`[01]–[18]`) | Phụ lục | Ngang | **3.7 pt** | **CHƯA ĐẠT 9pt (Chi tiết phụ lục)** |
| **11** | FIFO P2: Khóa đối soát (`[19]–[44]`) | Phụ lục | Ngang | **3.1 pt** | **CHƯA ĐẠT 9pt (Chi tiết phụ lục)** |
| **12** | FIFO P3: Snapshot & Commit (`[45]–[68]`) | Phụ lục | Ngang | **4.2 pt** | **CHƯA ĐẠT 9pt (Chi tiết phụ lục)** |
| **13** | *(Đối chứng)* Hóa đơn 2 phần cũ (`[15]–[69]`) | Đối chứng | Ngang | **2.4 pt** | **CHƯA ĐẠT (Chữ quá nhỏ)** |
