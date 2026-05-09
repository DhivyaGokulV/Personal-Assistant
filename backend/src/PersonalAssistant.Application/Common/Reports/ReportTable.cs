namespace PersonalAssistant.Application.Common.Reports;

public record ReportTable(string Title, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string?>> Rows);

public enum ReportFormat
{
    Json,
    Csv,
    Xlsx,
    Pdf
}

public record ExportedReport(byte[] Data, string ContentType, string FileName);

public interface IReportExportService
{
    ExportedReport ToCsv(ReportTable table);
    ExportedReport ToXlsx(ReportTable table);
    ExportedReport ToPdf(ReportTable table);

    ExportedReport Export(ReportTable table, ReportFormat format) => format switch
    {
        ReportFormat.Csv => ToCsv(table),
        ReportFormat.Xlsx => ToXlsx(table),
        ReportFormat.Pdf => ToPdf(table),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Use the JSON path directly.")
    };
}
