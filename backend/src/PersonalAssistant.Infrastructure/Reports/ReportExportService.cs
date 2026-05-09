using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using PersonalAssistant.Application.Common.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PersonalAssistant.Infrastructure.Reports;

public class ReportExportService : IReportExportService
{
    public ExportedReport ToCsv(ReportTable table)
    {
        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, new UTF8Encoding(true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var col in table.Columns) csv.WriteField(col);
            csv.NextRecord();
            foreach (var row in table.Rows)
            {
                foreach (var cell in row) csv.WriteField(cell ?? string.Empty);
                csv.NextRecord();
            }
            writer.Flush();
        }

        return new ExportedReport(
            ms.ToArray(),
            "text/csv",
            Sanitize(table.Title) + ".csv");
    }

    public ExportedReport ToXlsx(ReportTable table)
    {
        using var workbook = new XLWorkbook();
        var sheetName = Sanitize(table.Title);
        if (sheetName.Length > 31) sheetName = sheetName[..31];
        var ws = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Report" : sheetName);

        for (int c = 0; c < table.Columns.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = table.Columns[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            for (int c = 0; c < row.Count; c++)
            {
                ws.Cell(r + 2, c + 1).Value = row[c] ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return new ExportedReport(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Sanitize(table.Title) + ".xlsx");
    }

    public ExportedReport ToPdf(ReportTable table)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text(table.Title).SemiBold().FontSize(16);

                page.Content().PaddingVertical(8).Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        for (int i = 0; i < table.Columns.Count; i++)
                            cols.RelativeColumn();
                    });

                    t.Header(h =>
                    {
                        foreach (var col in table.Columns)
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(col).SemiBold();
                    });

                    foreach (var row in table.Rows)
                    {
                        foreach (var cell in row)
                        {
                            t.Cell()
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(4)
                                .Text(cell ?? string.Empty);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ").FontColor(Colors.Grey.Medium);
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"));
                    text.Span(" • Page ").FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return new ExportedReport(
            bytes,
            "application/pdf",
            Sanitize(table.Title) + ".pdf");
    }

    private static string Sanitize(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "report";
        var clean = new StringBuilder(title.Length);
        foreach (var ch in title)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_') clean.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '/' || ch == '\\') clean.Append('-');
        }
        var s = clean.ToString().Trim('-');
        return string.IsNullOrEmpty(s) ? "report" : s;
    }
}
