<#
.SYNOPSIS
    Script khởi tạo tài khoản an toàn cho hệ thống Bán hàng & Công nợ khách hàng.
.DESCRIPTION
    - Hiển thị server và database đích trước khi ghi.
    - Yêu cầu xác nhận từ người dùng.
    - Chỉ bổ sung bảng [dbo].[NguoiDungs] nếu chưa có.
    - Nhập ẩn hai mật khẩu (không in, không ghi log, không qua tham số dòng lệnh).
    - Tạo 'nvbh' và 'ktcn' nếu chưa tồn tại; không ghi đè nếu đã có.
    - Giữ nguyên 100% dữ liệu nghiệp vụ và các bảng khác.
.EXAMPLE
    .\Init-Accounts.ps1
.EXAMPLE
    .\Init-Accounts.ps1 -Database "QuanLyBanHangCongNo_Test"
#>
param(
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "QuanLyBanHangCongNo"
)

$toolProj = Join-Path $PSScriptRoot "..\..\BanHang.InitTool\BanHang.InitTool.csproj"
if (-not (Test-Path $toolProj)) {
    Write-Error "Không tìm thấy dự án công cụ tại: $toolProj"
    exit 1
}

Write-Host "Khởi chạy công cụ khởi tạo tài khoản an toàn..." -ForegroundColor Cyan
dotnet run --project "$toolProj" -- --server "$Server" --database "$Database"
