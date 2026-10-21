using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BanHang.UseCases.PluginInterfaces.Email;

namespace DuAn_BanHangVaCongNoKhachHang.Services;

/// <summary>
/// Bộ tạo email HTML tương thích Gmail bằng bảng (table layout) và CSS inline 100%.
/// Đảm bảo hiển thị tối ưu trên thiết bị di động, nền xám nhạt #f4f6f8, khung trắng tối đa 600px,
/// tiêu đề xanh navy #1e3a8a, khoảng cách thoáng, mã hóa an toàn dữ liệu động,
/// và loại bỏ hoàn toàn các thông báo trạng thái kỹ thuật khỏi email khách hàng.
/// </summary>
public static class EmailHtmlFormatter
{
    private class InvoiceRow
    {
        public string MaHoaDon { get; set; } = string.Empty;
        public string HanThanhToan { get; set; } = string.Empty;
        public string DuNo { get; set; } = string.Empty;
        public string TinhTrang { get; set; } = string.Empty;
    }

    public static string TaoHtmlEmail(ThongTinEmailGuiDto emailDto, string? senderName = null)
    {
        ArgumentNullException.ThrowIfNull(emailDto);

        string unitName = !string.IsNullOrWhiteSpace(senderName)
            ? senderName.Trim()
            : "Kế toán Công nợ";

        string rawSubject = !string.IsNullOrWhiteSpace(emailDto.TieuDe)
            ? emailDto.TieuDe.Trim()
            : "Thông báo đối soát công nợ";

        string encodedSubject = HtmlEncodeUtf8(rawSubject);
        string encodedUnitName = HtmlEncodeUtf8(unitName);

        string rawContent = emailDto.NoiDung?.Trim() ?? string.Empty;

        // Phân tách các phần trong nội dung đã duyệt
        var (greeting, introParagraphs, debtSummaryLines, invoices, closingParagraphs, signatureLines) =
            PhanTichNoiDung(rawContent);

        var bodyBuilder = new StringBuilder();

        // 1. Lời chào khách hàng
        if (!string.IsNullOrWhiteSpace(greeting))
        {
            bodyBuilder.Append($@"
              <div style=""font-size: 16px; font-weight: 600; color: #1e293b; margin: 0 0 16px 0;"">
                {HtmlEncodeUtf8(greeting)}
              </div>");
        }

        // 2. Nội dung nhắc nợ ngắn / Đoạn mở đầu
        foreach (var p in introParagraphs)
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                bodyBuilder.Append($@"
              <p style=""margin: 0 0 14px 0; color: #334155; font-size: 15px; line-height: 1.6;"">
                {HtmlEncodeUtf8(p)}
              </p>");
            }
        }

        // 3. Khung tổng công nợ (Summary Card nổi bật, phân biệt rõ khoản đến hạn vs chưa đến hạn)
        if (debtSummaryLines.Any())
        {
            bodyBuilder.Append(TaoKhungTongCongNoHtml(debtSummaryLines));
        }

        // 4. Bảng hóa đơn chi tiết (Mã HĐ, Hạn thanh toán, Số tiền còn nợ, Tình trạng)
        if (invoices.Any())
        {
            bodyBuilder.Append(TaoBangHoaDonHtml(invoices));
        }

        // 5. Lời kết và đề nghị thanh toán
        foreach (var p in closingParagraphs)
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                bodyBuilder.Append($@"
              <p style=""margin: 0 0 14px 0; color: #334155; font-size: 15px; line-height: 1.6;"">
                {HtmlEncodeUtf8(p)}
              </p>");
            }
        }

        // 6. Chữ ký kế toán
        string sigText = signatureLines.Any()
            ? string.Join("<br/>", signatureLines.Select(HtmlEncodeUtf8))
            : $"Trân trọng,<br/><strong style=\"color: #1e3a8a;\">{encodedUnitName}</strong>";

        bodyBuilder.Append($@"
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-top: 24px; padding-top: 16px; border-top: 1px solid #e2e8f0;"">
                <tr>
                  <td style=""color: #475569; font-size: 14px; line-height: 1.6;"">
                    {sigText}
                  </td>
                </tr>
              </table>");

        // Lắp ráp toàn bộ khung email HTML tương thích Gmail
        return $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
  <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{encodedSubject}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f4f6f8; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; -webkit-font-smoothing: antialiased; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f4f6f8; padding: 32px 12px;"">
    <tr>
      <td align=""center"" style=""padding: 0;"">
        <!-- KHUNG TRẮNG TỐI ĐA 600PX (ĐỌC TỐT TRÊN DI ĐỘNG & GMAIL) -->
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.04);"">
          <!-- HEADER: XANH NAVY (#1e3a8a) & TÊN ĐƠN VỊ GỬI -->
          <tr>
            <td style=""background-color: #1e3a8a; padding: 22px 28px; text-align: left;"">
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                <tr>
                  <td>
                    <div style=""font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.8px; color: #93c5fd; margin-bottom: 6px;"">
                      {encodedUnitName}
                    </div>
                    <div style=""font-size: 18px; font-weight: 700; color: #ffffff; line-height: 1.4;"">
                      {encodedSubject}
                    </div>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <!-- NỘI DUNG CHÍNH: KHOẢNG CÁCH THOÁNG, CHỮ DỄ ĐỌC -->
          <tr>
            <td style=""padding: 28px 28px 20px 28px; color: #334155; font-size: 15px; line-height: 1.6;"">
              {bodyBuilder}
            </td>
          </tr>
          <!-- FOOTER: LỊCH SỰ, BỎ HOÀN TOÀN TRẠNG THÁI KỸ THUẬT SMTP KHỎI THƯ KHÁCH HÀNG -->
          <tr>
            <td style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 16px 28px; text-align: center; color: #94a3b8; font-size: 12px; line-height: 1.5;"">
              Email thông báo đối soát công nợ tự động được gửi từ hệ thống quản trị bán hàng.<br/>
              Quý khách vui lòng liên hệ kế toán phụ trách nếu cần đối chiếu thêm chi tiết.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string TaoKhungTongCongNoHtml(List<string> lines)
    {
        var sb = new StringBuilder();
        sb.Append(@"
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-left: 4px solid #1e3a8a; border-radius: 6px; margin: 16px 0 20px 0; border-collapse: separate;"">
                <tr>
                  <td style=""padding: 16px 20px;"">");

        sb.Append(@"
                    <div style=""font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; color: #64748b; margin-bottom: 6px;"">
                      Tổng hợp công nợ đối soát
                    </div>");

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Tổng số dư công nợ
            if (line.Contains("tổng số dư công nợ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("tổng dư nợ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("tổng công nợ", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($@"
                    <div style=""font-size: 17px; font-weight: 700; color: #1e3a8a; margin-bottom: 6px;"">
                      {HtmlEncodeUtf8(line.TrimStart('-', '•', '+', '*').Trim())}
                    </div>");
            }
            // Khoản đã đến hạn (nhấn mạnh đề nghị thanh toán)
            else if (line.Contains("đã đến hạn", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("quá hạn", StringComparison.OrdinalIgnoreCase) && !line.Contains("nhóm", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($@"
                    <div style=""font-size: 14px; font-weight: 600; color: #b91c1c; margin-top: 4px;"">
                      • {HtmlEncodeUtf8(line.TrimStart('-', '•', '+', '*').Trim())}
                    </div>");
            }
            // Khoản chưa đến hạn (theo dõi đối chiếu)
            else if (line.Contains("chưa đến hạn", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("trong hạn", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($@"
                    <div style=""font-size: 14px; color: #475569; margin-top: 3px;"">
                      • {HtmlEncodeUtf8(line.TrimStart('-', '•', '+', '*').Trim())}
                    </div>");
            }
            // Tiền chưa phân bổ
            else if (line.Contains("chưa phân bổ", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($@"
                    <div style=""font-size: 13px; color: #b45309; background-color: #fffbeb; border: 1px solid #fef3c7; border-radius: 4px; padding: 6px 10px; margin-top: 8px;"">
                      * {HtmlEncodeUtf8(line.TrimStart('-', '•', '+', '*').Trim())}
                    </div>");
            }
            // Các nhóm thời hạn khác
            else
            {
                sb.Append($@"
                    <div style=""font-size: 13px; color: #64748b; margin-top: 2px;"">
                      {HtmlEncodeUtf8(line.TrimStart('•', '+', '*'))}
                    </div>");
            }
        }

        sb.Append(@"
                  </td>
                </tr>
              </table>");

        return sb.ToString();
    }

    private static string TaoBangHoaDonHtml(List<InvoiceRow> invoices)
    {
        var sb = new StringBuilder();
        sb.Append(@"
              <div style=""font-size: 14px; font-weight: 700; color: #1e293b; margin-top: 18px; margin-bottom: 8px;"">
                Bảng chi tiết các hóa đơn:
              </div>
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""border-collapse: collapse; font-size: 13px; margin-bottom: 18px; border: 1px solid #e2e8f0; border-radius: 6px; overflow: hidden;"">
                <thead>
                  <tr style=""background-color: #f1f5f9; border-bottom: 2px solid #cbd5e1;"">
                    <th style=""padding: 10px 12px; text-align: left; font-weight: 600; color: #1e293b; border-bottom: 2px solid #cbd5e1;"">Mã HĐ</th>
                    <th style=""padding: 10px 12px; text-align: center; font-weight: 600; color: #1e293b; border-bottom: 2px solid #cbd5e1;"">Hạn thanh toán</th>
                    <th style=""padding: 10px 12px; text-align: right; font-weight: 600; color: #1e293b; border-bottom: 2px solid #cbd5e1;"">Số tiền còn nợ</th>
                    <th style=""padding: 10px 12px; text-align: center; font-weight: 600; color: #1e293b; border-bottom: 2px solid #cbd5e1;"">Tình trạng</th>
                  </tr>
                </thead>
                <tbody>");

        int index = 0;
        foreach (var inv in invoices)
        {
            string bg = index % 2 == 0 ? "#ffffff" : "#f8fafc";
            index++;

            // Phân biệt tình trạng nợ: quá hạn (đỏ/hồng), đến hạn hôm nay (vàng hổ phách), chưa đến hạn (xanh lá nhạt/xám)
            string badgeStyle;
            if (inv.TinhTrang.Contains("Quá hạn", StringComparison.OrdinalIgnoreCase))
            {
                badgeStyle = "display: inline-block; padding: 3px 8px; font-size: 11px; font-weight: 600; color: #991b1b; background-color: #fee2e2; border: 1px solid #fecaca; border-radius: 12px; white-space: nowrap;";
            }
            else if (inv.TinhTrang.Contains("Đến hạn", StringComparison.OrdinalIgnoreCase))
            {
                badgeStyle = "display: inline-block; padding: 3px 8px; font-size: 11px; font-weight: 600; color: #92400e; background-color: #fef3c7; border: 1px solid #fde68a; border-radius: 12px; white-space: nowrap;";
            }
            else
            {
                badgeStyle = "display: inline-block; padding: 3px 8px; font-size: 11px; font-weight: 500; color: #065f46; background-color: #d1fae5; border: 1px solid #a7f3d0; border-radius: 12px; white-space: nowrap;";
            }

            sb.Append($@"
                  <tr style=""border-bottom: 1px solid #e2e8f0; background-color: {bg};"">
                    <td style=""padding: 10px 12px; font-weight: 600; color: #0f172a; border-bottom: 1px solid #e2e8f0;"">
                      {HtmlEncodeUtf8(inv.MaHoaDon)}
                    </td>
                    <td style=""padding: 10px 12px; text-align: center; color: #475569; border-bottom: 1px solid #e2e8f0;"">
                      {HtmlEncodeUtf8(inv.HanThanhToan)}
                    </td>
                    <td style=""padding: 10px 12px; text-align: right; font-weight: 600; color: #0f172a; border-bottom: 1px solid #e2e8f0;"">
                      {HtmlEncodeUtf8(inv.DuNo)}
                    </td>
                    <td style=""padding: 10px 12px; text-align: center; border-bottom: 1px solid #e2e8f0;"">
                      <span style=""{badgeStyle}"">{HtmlEncodeUtf8(inv.TinhTrang)}</span>
                    </td>
                  </tr>");
        }

        sb.Append(@"
                </tbody>
              </table>");

        return sb.ToString();
    }

    private static (
        string Greeting,
        List<string> IntroParagraphs,
        List<string> DebtSummaryLines,
        List<InvoiceRow> Invoices,
        List<string> ClosingParagraphs,
        List<string> SignatureLines
    ) PhanTichNoiDung(string rawContent)
    {
        var rawLines = rawContent
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(l => l.Trim())
            .ToList();

        string greeting = string.Empty;
        var introParagraphs = new List<string>();
        var debtSummaryLines = new List<string>();
        var invoices = new List<InvoiceRow>();
        var closingParagraphs = new List<string>();
        var signatureLines = new List<string>();

        int stage = 0; // 0: Greeting/Intro, 1: Summary, 2: Invoices, 3: Closing/Signature

        for (int i = 0; i < rawLines.Count; i++)
        {
            string line = rawLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Bỏ dòng tiêu đề nếu AI hoặc người dùng để sót lại trong nội dung
            if (line.StartsWith("[TIÊU ĐỀ]:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Tiêu đề:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Dòng phân cách ASCII cũ (nếu có)
            if (Regex.IsMatch(line, @"^[-=—_]{4,}$") || line.Contains("--- BẢNG SỐ LIỆU", StringComparison.OrdinalIgnoreCase))
            {
                if (stage < 1) stage = 1;
                continue;
            }

            // Nhận diện lời chào
            if (string.IsNullOrEmpty(greeting) && line.StartsWith("Kính gửi", StringComparison.OrdinalIgnoreCase))
            {
                greeting = line;
                continue;
            }

            // Nhận diện bắt đầu phần Tổng nợ / Báo cáo thời hạn
            if (line.Contains("Tổng số dư công nợ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("tổng dư nợ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("TỔNG HỢP CÔNG NỢ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Tổng hợp các khoản công nợ theo thời hạn", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Chi tiết phân bổ theo các nhóm hạn nợ", StringComparison.OrdinalIgnoreCase))
            {
                stage = 1;
                debtSummaryLines.Add(line);
                continue;
            }

            // Nhận diện dòng bắt đầu Bảng hóa đơn
            if (line.Contains("Bảng chi tiết các hóa đơn", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("BẢNG HÓA ĐƠN", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Danh sách các hóa đơn", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Danh sách hóa đơn", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Mã HĐ |", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Số HĐ |", StringComparison.OrdinalIgnoreCase))
            {
                stage = 2;
                continue;
            }

            // Nhận diện kết thúc / Đề nghị thanh toán / Chữ ký
            if (line.StartsWith("Kính đề nghị", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Quý khách vui lòng", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Để đảm bảo", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Các khoản công nợ", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Các hóa đơn", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Nếu Quý khách", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Nếu có bất kỳ", StringComparison.OrdinalIgnoreCase))
            {
                stage = 3;
                closingParagraphs.Add(line);
                continue;
            }

            if (line.StartsWith("Trân trọng", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Bộ phận Kế toán", StringComparison.OrdinalIgnoreCase))
            {
                stage = 3;
                signatureLines.Add(line);
                continue;
            }

            // Xử lý theo từng giai đoạn
            if (stage == 1)
            {
                // Trong giai đoạn summary, nếu gặp hóa đơn dạng pipe hoặc bullet thì chuyển sang stage 2
                if (KiemTraDongHoaDon(line))
                {
                    stage = 2;
                    ThemHoaDon(line, invoices);
                }
                else
                {
                    debtSummaryLines.Add(line);
                }
            }
            else if (stage == 2)
            {
                if (KiemTraDongHoaDon(line))
                {
                    ThemHoaDon(line, invoices);
                }
                else
                {
                    // Nếu là ghi chú đối soát tiền chưa phân bổ nằm sau hóa đơn
                    if (line.Contains("chưa phân bổ", StringComparison.OrdinalIgnoreCase))
                    {
                        debtSummaryLines.Add(line);
                    }
                    else
                    {
                        stage = 3;
                        closingParagraphs.Add(line);
                    }
                }
            }
            else if (stage == 3)
            {
                if (line.StartsWith("Trân trọng", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("Bộ phận Kế toán", StringComparison.OrdinalIgnoreCase))
                {
                    signatureLines.Add(line);
                }
                else
                {
                    closingParagraphs.Add(line);
                }
            }
            else
            {
                // stage == 0: Đoạn mở đầu
                introParagraphs.Add(line);
            }
        }

        return (greeting, introParagraphs, debtSummaryLines, invoices, closingParagraphs, signatureLines);
    }

    private static bool KiemTraDongHoaDon(string line)
    {
        if (line.Contains('|'))
        {
            var parts = line.Split('|');
            if (parts.Length >= 4 && !parts[0].Contains("Mã HĐ", StringComparison.OrdinalIgnoreCase) && !parts[0].Contains("Số HĐ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (Regex.IsMatch(line, @"^\+?\s*HĐ\s+[A-Za-z0-9_-]+", RegexOptions.IgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static void ThemHoaDon(string line, List<InvoiceRow> list)
    {
        // Định dạng 1: Pipe table: Mã HĐ | Hạn thanh toán | Số tiền còn nợ | Tình trạng
        if (line.Contains('|'))
        {
            var parts = line.Split('|').Select(p => p.Trim()).ToArray();
            if (parts.Length >= 4)
            {
                if (parts[0].Contains("Mã", StringComparison.OrdinalIgnoreCase) || parts[0].Contains("Số HĐ", StringComparison.OrdinalIgnoreCase))
                {
                    return; // Header line
                }

                if (parts.Length == 4)
                {
                    list.Add(new InvoiceRow
                    {
                        MaHoaDon = parts[0],
                        HanThanhToan = parts[1],
                        DuNo = parts[2],
                        TinhTrang = parts[3]
                    });
                    return;
                }
                else if (parts.Length >= 5)
                {
                    // Format: Số HĐ | Ngày lập | Hạn TT | Dư nợ | Tình trạng
                    list.Add(new InvoiceRow
                    {
                        MaHoaDon = parts[0],
                        HanThanhToan = parts[2],
                        DuNo = parts[3],
                        TinhTrang = parts[4]
                    });
                    return;
                }
            }
        }

        // Định dạng 2: + HĐ HD001 (Ngày lập: 01/10/2026, Hạn trả: 15/10/2026): 50,000,000 đ [Quá hạn 6 ngày]
        var match = Regex.Match(line, @"^\+?\s*HĐ\s+([A-Za-z0-9_-]+).*?Hạn\s*(?:thanh toán|trả):\s*([0-9/]+).*?:\s*([0-9.,]+)\s*đ?\s*\[(.*?)\]", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            list.Add(new InvoiceRow
            {
                MaHoaDon = match.Groups[1].Value.Trim(),
                HanThanhToan = match.Groups[2].Value.Trim(),
                DuNo = match.Groups[3].Value.Trim() + " đ",
                TinhTrang = match.Groups[4].Value.Trim()
            });
            return;
        }

        // Định dạng 3: - HĐ HD001: Hạn thanh toán 15/10/2026 - Còn nợ 50,000,000 đ - Quá hạn 6 ngày
        var match2 = Regex.Match(line, @"^[-+*]?\s*HĐ\s*([A-Za-z0-9_-]+).*?(?:Hạn|Hạn TT|Hạn thanh toán)[:\s]+([0-9/]+).*?([0-9.,]+)\s*đ?.*?(Quá hạn.*|Chưa đến hạn|Đến hạn.*|Trong hạn)", RegexOptions.IgnoreCase);
        if (match2.Success)
        {
            list.Add(new InvoiceRow
            {
                MaHoaDon = match2.Groups[1].Value.Trim(),
                HanThanhToan = match2.Groups[2].Value.Trim(),
                DuNo = match2.Groups[3].Value.Trim() + " đ",
                TinhTrang = match2.Groups[4].Value.Trim()
            });
        }
    }

    /// <summary>
    /// Mã hóa an toàn các ký tự điều khiển HTML (&lt;, &gt;, &amp;, &quot;, &#39;) để chống XSS
    /// nhưng bảo tồn toàn vẹn các ký tự tiếng Việt có dấu trong email UTF-8.
    /// </summary>
    public static string HtmlEncodeUtf8(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}

