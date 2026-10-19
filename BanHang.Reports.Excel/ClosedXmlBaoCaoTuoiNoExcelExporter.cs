using System;
using System.IO;
using System.Linq;
using BanHang.CoreBusiness;
using BanHang.UseCases.BaoCaoTuoiNoScreen;
using BanHang.UseCases.PluginInterfaces.Reports;
using ClosedXML.Excel;

namespace BanHang.Reports.Excel;

public class ClosedXmlBaoCaoTuoiNoExcelExporter : IBaoCaoTuoiNoExcelExporter
{
    private const string CurrencyFormat = "#,##0 \"đ\"";
    private const string DateFormat = "dd/MM/yyyy";

    public byte[] ExportBaoCaoTuoiNo(BaoCaoTuoiNoResult baoCao)
    {
        if (baoCao == null) throw new ArgumentNullException(nameof(baoCao));

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("TuoiNoKhachHang");
        ws.ShowGridLines = true;

        // 1. Tiêu đề báo cáo
        var titleCell = ws.Cell("A2");
        titleCell.Value = "BÁO CÁO PHÂN TÍCH TUỔI NỢ KHÁCH HÀNG";
        titleCell.Style.Font.FontSize = 16;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range("A2:I2").Merge();
        ws.Row(2).Height = 30;

        // 2. Ngày chốt số liệu (lưu giá trị DateTime thực tế trong ô B3 có định dạng ngày)
        ws.Cell("A3").Value = "Ngày chốt số liệu:";
        ws.Cell("A3").Style.Font.Bold = true;
        ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var dateCell = ws.Cell("B3");
        dateCell.Value = baoCao.NgayBaoCao.Date;
        dateCell.Style.DateFormat.Format = DateFormat;
        dateCell.Style.Font.Bold = true;
        dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        var noteUnitCell = ws.Cell("I3");
        noteUnitCell.Value = "Đơn vị tính: VNĐ";
        noteUnitCell.Style.Font.Italic = true;
        noteUnitCell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
        noteUnitCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // 3. Tiêu đề cột (Row 5)
        int headerRowIndex = 5;
        string[] headers =
        [
            "STT",
            "Mã KH",
            "Tên khách hàng",
            "Hạn mức",
            "0–30 ngày",
            "31–60 ngày",
            "61–90 ngày",
            "Trên 90 ngày",
            "Tổng công nợ"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRowIndex, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B"); // Slate dark
            cell.Style.Alignment.Horizontal = i switch
            {
                0 or 1 => XLAlignmentHorizontalValues.Center,
                2 => XLAlignmentHorizontalValues.Left,
                _ => XLAlignmentHorizontalValues.Right
            };
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#0F172A");
        }
        ws.Row(headerRowIndex).Height = 28;

        // Cố định hàng tiêu đề từ hàng 5 trở lên
        ws.SheetView.FreezeRows(headerRowIndex);

        // 4. Dòng dữ liệu
        int currentRow = headerRowIndex + 1;

        if (baoCao.DanhSach == null || !baoCao.DanhSach.Any())
        {
            var emptyCell = ws.Cell(currentRow, 1);
            emptyCell.Value = "Không có dữ liệu công nợ khách hàng tại ngày chốt báo cáo.";
            emptyCell.Style.Font.Italic = true;
            emptyCell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            emptyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            emptyCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            var emptyRange = ws.Range(currentRow, 1, currentRow, headers.Length);
            emptyRange.Merge();
            emptyRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            emptyRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            ws.Row(currentRow).Height = 24;
            currentRow++;
        }
        else
        {
            int stt = 1;
            foreach (var item in baoCao.DanhSach)
            {
                var row = ws.Row(currentRow);
                var isEven = stt % 2 == 0;
                var rowBgColor = isEven ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                // STT (số)
                var cellStt = ws.Cell(currentRow, 1);
                cellStt.Value = stt++;
                cellStt.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Mã KH (text)
                var cellMa = ws.Cell(currentRow, 2);
                cellMa.SetValue(SanitizeText(item.MaKhachHang));
                cellMa.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Tên KH (text, wrap text, chống formula injection)
                var cellTen = ws.Cell(currentRow, 3);
                cellTen.SetValue(SanitizeText(item.TenKhachHang));
                cellTen.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cellTen.Style.Alignment.WrapText = true;

                // Các cột số tiền (lưu kiểu số, định dạng có phân cách hàng nghìn)
                SetCurrencyCell(ws.Cell(currentRow, 4), item.HanMucCongNo);
                SetCurrencyCell(ws.Cell(currentRow, 5), item.TuoiNo_0_30);
                SetCurrencyCell(ws.Cell(currentRow, 6), item.TuoiNo_31_60);
                SetCurrencyCell(ws.Cell(currentRow, 7), item.TuoiNo_61_90);
                SetCurrencyCell(ws.Cell(currentRow, 8), item.TuoiNo_Tren_90);

                // Tổng công nợ (bold)
                var cellTong = ws.Cell(currentRow, 9);
                SetCurrencyCell(cellTong, item.TongCongNo);
                cellTong.Style.Font.Bold = true;

                var dataRange = ws.Range(currentRow, 1, currentRow, headers.Length);
                dataRange.Style.Fill.BackgroundColor = rowBgColor;
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                currentRow++;
            }
        }

        // 5. Hàng tổng cộng
        int totalRowIndex = currentRow;
        var totalLabelCell = ws.Cell(totalRowIndex, 1);
        totalLabelCell.Value = "TỔNG CỘNG";
        totalLabelCell.Style.Font.Bold = true;
        totalLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(totalRowIndex, 1, totalRowIndex, 3).Merge();

        decimal tongHanMuc = baoCao.DanhSach?.Sum(x => x.HanMucCongNo) ?? 0;
        SetCurrencyCell(ws.Cell(totalRowIndex, 4), tongHanMuc, isBold: true);
        SetCurrencyCell(ws.Cell(totalRowIndex, 5), baoCao.Tong_TuoiNo_0_30, isBold: true);
        SetCurrencyCell(ws.Cell(totalRowIndex, 6), baoCao.Tong_TuoiNo_31_60, isBold: true);
        SetCurrencyCell(ws.Cell(totalRowIndex, 7), baoCao.Tong_TuoiNo_61_90, isBold: true);
        SetCurrencyCell(ws.Cell(totalRowIndex, 8), baoCao.Tong_TuoiNo_Tren_90, isBold: true);

        var cellTotalGrand = ws.Cell(totalRowIndex, 9);
        SetCurrencyCell(cellTotalGrand, baoCao.Tong_CongNo, isBold: true);
        cellTotalGrand.Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

        var totalRange = ws.Range(totalRowIndex, 1, totalRowIndex, headers.Length);
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#475569");
        totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        totalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#0F172A");
        totalRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#CBD5E1");
        ws.Row(totalRowIndex).Height = 24;

        // 6. Khung Tiền thu chưa phân bổ
        int unallocatedRow = totalRowIndex + 2;
        var unallocatedRange = ws.Range(unallocatedRow, 1, unallocatedRow, headers.Length);
        unallocatedRange.Merge();
        var unallocatedCell = ws.Cell(unallocatedRow, 1);
        unallocatedCell.Value = $"Tiền thu chưa phân bổ tính đến ngày {baoCao.NgayBaoCao:dd/MM/yyyy}: {baoCao.TienThuChuaPhanBo:#,##0} đ (Lưu riêng trên phiếu thu, không tự trừ vào dư nợ hóa đơn)";
        unallocatedCell.Style.Font.Bold = true;
        unallocatedCell.Style.Font.FontColor = XLColor.FromHtml("#92400E"); // Dark amber
        unallocatedRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7"); // Light amber
        unallocatedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        unallocatedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#FCD34D");
        unallocatedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        unallocatedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(unallocatedRow).Height = 24;

        // 7. Ghi chú quy ước nghiệp vụ
        int noteRow = unallocatedRow + 2;
        var noteTitleCell = ws.Cell(noteRow, 1);
        noteTitleCell.Value = "Ghi chú quy ước nghiệp vụ:";
        noteTitleCell.Style.Font.Bold = true;
        noteTitleCell.Style.Font.FontSize = 10;
        noteTitleCell.Style.Font.FontColor = XLColor.FromHtml("#334155");

        string[] notes =
        [
            "1. Quy ước nhóm: Nhóm 0–30 bao gồm các khoản chưa đến hạn và các khoản quá hạn tối đa 30 ngày tính đến ngày chốt báo cáo.",
            "2. Phạm vi thời gian: Báo cáo phản ánh các hóa đơn và phân bổ thanh toán đã ghi nhận đến hết ngày báo cáo (chưa tái dựng lại FIFO cho phiếu thu nhập lùi ngày).",
            "3. Nguyên tắc công nợ: Tổng công nợ của mỗi khách hàng bằng tổng 4 nhóm tuổi nợ (0-30 + 31-60 + 61-90 + Trên 90 ngày)."
        ];

        for (int i = 0; i < notes.Length; i++)
        {
            int r = noteRow + 1 + i;
            var cell = ws.Cell(r, 1);
            cell.Value = notes[i];
            cell.Style.Font.Italic = true;
            cell.Style.Font.FontSize = 9.5;
            cell.Style.Font.FontColor = XLColor.FromHtml("#475569");
            ws.Range(r, 1, r, headers.Length).Merge();
        }

        // 8. Căn chỉnh độ rộng cột tối thiểu và tự động
        ws.Columns(1, headers.Length).AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 8);   // STT
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 14);  // Mã KH
        ws.Column(3).Width = Math.Max(ws.Column(3).Width, 32);  // Tên KH
        ws.Column(4).Width = Math.Max(ws.Column(4).Width, 16);  // Hạn mức
        ws.Column(5).Width = Math.Max(ws.Column(5).Width, 16);  // 0-30
        ws.Column(6).Width = Math.Max(ws.Column(6).Width, 16);  // 31-60
        ws.Column(7).Width = Math.Max(ws.Column(7).Width, 16);  // 61-90
        ws.Column(8).Width = Math.Max(ws.Column(8).Width, 16);  // Trên 90
        ws.Column(9).Width = Math.Max(ws.Column(9).Width, 18);  // Tổng công nợ

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void SetCurrencyCell(IXLCell cell, decimal amount, bool isBold = false)
    {
        cell.Value = amount;
        cell.Style.NumberFormat.Format = CurrencyFormat;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        if (isBold)
        {
            cell.Style.Font.Bold = true;
        }
    }

    private static string SanitizeText(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var trimmed = input.Trim();
        // Chống Formula Injection: nếu bắt đầu bằng các ký tự công thức, thêm dấu nháy đơn
        if (trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@'))
        {
            return "'" + trimmed;
        }
        return trimmed;
    }
}
