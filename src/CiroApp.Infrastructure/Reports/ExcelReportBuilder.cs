using CiroApp.Application.Abstractions;
using CiroApp.Domain.Models;
using CiroApp.Domain.Reports;
using ClosedXML.Excel;

namespace CiroApp.Infrastructure.Reports;

public sealed class ExcelReportBuilder : IReportBuilder
{
    private const string TarihBicimi = "d.M.yyyy";
    private const string TutarBicimi = "#,##0.00";

    public byte[] Build(ReportRange range, IReadOnlyList<CiroDay> days)
    {
        var byDate = days.ToDictionary(d => d.Tarih);
        var title = ReportNaming.Title(range);

        using var wb = new XLWorkbook();
        BuildCiro(wb.AddWorksheet("Ciro"), range, byDate, title);
        BuildHavale(wb.AddWorksheet("Havale-EFT"), range, byDate, title);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildCiro(IXLWorksheet ws, ReportRange range, Dictionary<DateOnly, CiroDay> byDate, string title)
    {
        ws.Cell(1, 1).Value = title;
        string[] basliklar = ["Tarih", "Nakit", "Kredi kartı", "Havale", "Günlük Toplam"];
        for (var c = 0; c < basliklar.Length; c++) ws.Cell(2, c + 1).Value = basliklar[c];
        ws.Range(2, 1, 2, 5).Style.Font.Bold = true;

        decimal nakit = 0, kart = 0, havale = 0, toplam = 0;
        var row = 3;
        foreach (var tarih in range.Days())
        {
            TarihYaz(ws.Cell(row, 1), tarih);
            if (byDate.TryGetValue(tarih, out var gun))
            {
                TutarYaz(ws.Cell(row, 2), gun.Nakit);
                TutarYaz(ws.Cell(row, 3), gun.KrediKarti);
                TutarYaz(ws.Cell(row, 4), gun.HavaleToplam);
                TutarYaz(ws.Cell(row, 5), gun.GunlukToplam);
                nakit += gun.Nakit;
                kart += gun.KrediKarti;
                havale += gun.HavaleToplam;
                toplam += gun.GunlukToplam;
            }

            row++;
        }

        ws.Cell(row, 1).Value = "Genel Toplam";
        TutarYaz(ws.Cell(row, 2), nakit);
        TutarYaz(ws.Cell(row, 3), kart);
        TutarYaz(ws.Cell(row, 4), havale);
        TutarYaz(ws.Cell(row, 5), toplam);
        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    private static void BuildHavale(IXLWorksheet ws, ReportRange range, Dictionary<DateOnly, CiroDay> byDate, string title)
    {
        ws.Cell(1, 1).Value = title;
        ws.Cell(2, 1).Value = "Tarih";
        ws.Cell(2, 2).Value = "Havale";
        ws.Cell(2, 3).Value = "Müşteri";
        ws.Range(2, 1, 2, 4).Style.Font.Bold = true;

        decimal genel = 0;
        var row = 3;
        foreach (var tarih in range.Days())
        {
            TarihYaz(ws.Cell(row, 1), tarih);
            if (byDate.TryGetValue(tarih, out var gun) && gun.Havaleler.Count > 0)
            {
                foreach (var h in gun.Havaleler)
                {
                    TutarYaz(ws.Cell(row, 2), h.Tutar);
                    // Kişi adı kullanıcı girdisi: formül olarak yorumlanmasın diye açıkça metin yazılır.
                    ws.Cell(row, 3).SetValue(h.KisiAdi);
                    row++;
                }

                ws.Cell(row, 3).Value = "Günlük Toplam";
                TutarYaz(ws.Cell(row, 4), gun.HavaleToplam);
                ws.Range(row, 3, row, 4).Style.Font.Bold = true;
                genel += gun.HavaleToplam;
            }

            row++;
        }

        ws.Cell(row, 1).Value = "Genel Toplam";
        TutarYaz(ws.Cell(row, 2), genel);
        ws.Range(row, 1, row, 2).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    private static void TarihYaz(IXLCell cell, DateOnly tarih)
    {
        cell.Value = tarih.ToDateTime(TimeOnly.MinValue);
        cell.Style.DateFormat.Format = TarihBicimi;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void TutarYaz(IXLCell cell, decimal tutar)
    {
        cell.Value = tutar;
        cell.Style.NumberFormat.Format = TutarBicimi;
    }
}
