# DuAn_BanHangVaCongNoKhachHang_CuoiKy

## Đồ án Capstone ECO2415 — Bán hàng & Công nợ Khách hàng
- **Học phần**: Lập trình Ứng dụng Web / Đồ án Capstone
- **Nền tảng**: .NET 8 / Blazor Web App (Interactive Server)
- **Kiến trúc**: Clean Architecture 4 tầng (`CoreBusiness → UseCases → Plugins/DataStore → WebApp`)
- **Cơ sở dữ liệu**: SQL Server + Dapper (Tham số hóa 100%)
- **Hệ thống Quản lý**: Bán hàng B2B, Kiểm soát Hạn mức Tín dụng (Credit Limit) và Cấn trừ nợ FIFO.

---

### Cấu trúc dự án (Visual Studio Solution: `DuAn_BanHangVaCongNoKhachHang.sln`)
1. `DuAn_BanHangVaCongNoKhachHang`: WebApp Blazor giao diện người dùng.
2. *(Sẽ bổ sung theo lộ trình 4 tuần)*: `CoreBusiness`, `UseCases`, `Plugins.DataStore.SQL`.
