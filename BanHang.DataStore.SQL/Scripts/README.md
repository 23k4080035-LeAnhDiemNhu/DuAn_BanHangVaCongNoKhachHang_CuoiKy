# HƯỚNG DẪN THỰC THI SCRIPT CƠ SỞ DỮ LIỆU SQL SERVER

Dự án Capstone: **Quản lý Bán hàng & Công nợ Khách hàng (Đề 18 - ECO2415)**

---

## 1. PHÂN BIỆT RÕ CÁC NHÓM SCRIPT

| Tên tệp script | Mục đích | Khi nào sử dụng? | Nguyên tắc an toàn |
| :--- | :--- | :--- | :--- |
| **`01_Safe_CreateSchema.sql`** | Khởi tạo Schema 6 bảng nghiệp vụ | **Cài mới** trên database trống (hoặc kiểm tra tính tương thích cấu trúc) | Idempotent, **KHÔNG DROP/DELETE**, có kiểm tra và bổ sung `SoNgayNo`, chặn chạy trên DB hệ thống. |
| **`02_Safe_CreateNguoiDungs.sql`** | Khởi tạo bảng tài khoản phân quyền | **Cài mới** hoặc kiểm tra bảng `NguoiDungs` | Idempotent, kiểm tra đủ 7 cột, không lưu mật khẩu rõ. |
| **`03_Safe_SeedSampleData.sql`** | Nạp dữ liệu mẫu ban đầu | **Nạp dữ liệu mẫu** cho database mới | **Chỉ nạp khi các bảng còn trống**, chạy trong transaction, tự động map ID động. |
| **`Migration_Add_SoNgayNo_To_KhachHangs.sql`** | Nâng cấp bảng `KhachHangs` | **Nâng cấp CSDL cũ** đang có dữ liệu thật | Thêm cột `SoNgayNo` với `DEFAULT (0)` và `CHECK (>= 0)`, **bảo toàn 100% dữ liệu cũ, không sửa hạn thanh toán hóa đơn cũ**. |

---

## 2. QUY TRÌNH THỰC THI CHI TIẾT THEO TỪNG KỊCH BẢN

### Kịch bản 1: Cài đặt mới hoàn toàn trên Database trống
Áp dụng khi chấm thi, kiểm thử trên môi trường mới hoặc bàn giao hệ thống:

```sql
-- Bước 1: Tạo Database mới trong SSMS hoặc sqlcmd
CREATE DATABASE [TenDatabaseMoi];
GO
USE [TenDatabaseMoi];
GO

-- Bước 2: Chạy lần lượt 3 script cài đặt an toàn
-- 1. Chạy 01_Safe_CreateSchema.sql
-- 2. Chạy 02_Safe_CreateNguoiDungs.sql
-- 3. Chạy 03_Safe_SeedSampleData.sql
```

Hoặc qua dòng lệnh `sqlcmd`:
```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d TenDatabaseMoi -i 01_Safe_CreateSchema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d TenDatabaseMoi -i 02_Safe_CreateNguoiDungs.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d TenDatabaseMoi -i 03_Safe_SeedSampleData.sql
```

Sau khi nạp xong, chạy script PowerShell khởi tạo mật khẩu bảo mật (băm muối PBKDF2):
```powershell
.\Init-Accounts.ps1 -Database "TenDatabaseMoi"
```

---

### Kịch bản 2: Nâng cấp Cơ sở dữ liệu cũ đang có dữ liệu thực tế
Áp dụng khi hệ thống đang vận hành và cần bổ sung trường `SoNgayNo` (Credit Terms) cho bảng `KhachHangs`:

1. Mở SSMS hoặc `sqlcmd` kết nối đến database cần nâng cấp.
2. Thực thi duy nhất tệp:
   ```sql
   USE [TenDatabaseHienHuu];
   GO
   -- Chạy tệp: Migration_Add_SoNgayNo_To_KhachHangs.sql
   ```
3. Script sẽ:
   - Thêm cột `SoNgayNo INT NOT NULL DEFAULT (0)`.
   - Giữ nguyên toàn bộ số liệu khách hàng hiện tại (mặc định gán 0 ngày = thanh toán cùng ngày).
   - Tuyệt đối không can thiệp hay thay đổi `HanThanhToan` của bất kỳ hóa đơn nào đã lưu trước đây.

---

### Kịch bản 3: Các tệp script nguyên bản phục vụ đối soát lịch sử
- `01_CreateDatabaseAndTables.sql`: Bản khởi tạo ban đầu phục vụ đối soát lịch sử đồ án.
- `02_SeedData.sql`: Bản dữ liệu mẫu ban đầu.
- `03_CreateNguoiDungsTable.sql`: Bản script idempotent đầu tiên cho bảng Người dùng.
- `SQLQuery1.sql` đến `SQLQuery5.sql`: Các câu truy vấn kiểm thử lưu trữ đối chiếu.
*(Được giữ nguyên trạng trong repository để bảo đảm tính truy nguyên lịch sử đồ án).*
