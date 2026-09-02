using System;
using System.IO;
using System.Linq;
using System.Text;
using AutoSalesApp.Data;
using Dapper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using OfficeOpenXml;
using SkiaSharp;

namespace AutoSalesApp.Services;

public static class ExportService
{
    // ---------------- CSV ----------------

    /// entity: "producers" | "models" | "offers" | "pricelist" | "clients" | "sales"
    public static string ExportCsv(string path, string entity)
    {
        using var connection = Database.GetConnection();
        var sb = new StringBuilder();

        switch (entity)
        {
            case "producers":
                sb.AppendLine("Код;Название;Телефон;Email;Web-сайт");
                foreach (var r in connection.Query("SELECT * FROM Producer"))
                    sb.AppendLine($"{r.CompanyCode};{r.CompanyName};{r.Phone};{r.Email};{r.Website ?? ""}");
                break;
            case "models":
                sb.AppendLine("Код;Наименование;Цвет;Обивка;Мощность;Кол-во дверей;КПП");
                foreach (var r in connection.Query("SELECT * FROM Model"))
                    sb.AppendLine($"{r.ModelCode};{r.ModelName};{r.Color};{r.Upholstery};{r.MotorPower};{r.DoorCount};{r.Transmission}");
                break;
            case "offers":
                sb.AppendLine("Код поставщика;Код модели");
                foreach (var r in connection.Query(
                             @"SELECT p.CompanyCode, m.ModelCode FROM Offer of
                               JOIN Producer p ON p.ProducerId = of.ProducerId
                               JOIN Model m ON m.ModelId = of.ModelId"))
                    sb.AppendLine($"{r.CompanyCode};{r.ModelCode}");
                break;
            case "pricelist":
                sb.AppendLine("Код модели;Год;Цена;Подготовка;Транспорт");
                foreach (var r in connection.Query(
                             @"SELECT m.ModelCode, p.YearOfManufacture, p.Price, p.PrepCost, p.TransportCost
                               FROM PriceList p JOIN Model m ON m.ModelId = p.ModelId"))
                    sb.AppendLine($"{r.ModelCode};{r.YearOfManufacture};{r.Price};{r.PrepCost};{r.TransportCost}");
                break;
            case "clients":
                sb.AppendLine("ФИО;Телефон;Адрес");
                foreach (var r in connection.Query("SELECT * FROM Client"))
                    sb.AppendLine($"{r.FIO};{r.Phone};{r.Address}");
                break;
            case "sales":
                sb.AppendLine("Договор;ФИО;Модель;Дата;Полная стоимость");
                foreach (var r in connection.Query(
                             @"SELECT o.OrderNumber, c.FIO, m.ModelCode, o.OrderDate, o.TotalCost
                               FROM [Order] o
                               JOIN Client c ON c.ClientId = o.ClientId
                               JOIN Model m ON m.ModelId = o.ModelId"))
                    sb.AppendLine($"{r.OrderNumber};{r.FIO};{r.ModelCode};{r.OrderDate};{r.TotalCost}");
                break;
        }

        File.WriteAllText(path, sb.ToString());
        return $"CSV-файл сохранён: {path}";
    }

    // ---------------- Excel ----------------

    public static string ExportExcel(string path)
    {
        ImportService.SetExcelLicense();
        using var connection = Database.GetConnection();
        using var package = new ExcelPackage();

        AddSheet(package, "Поставщики",
            new[] { "Код фирмы", "Название", "Телефон", "Email", "Web-сайт" },
            connection.Query("SELECT * FROM Producer").Select(r =>
                new object?[] { r.CompanyCode, r.CompanyName, r.Phone, r.Email, r.Website }),
            moneyColumns: Array.Empty<int>());

        AddSheet(package, "Модели",
            new[] { "Код модели", "Наименование", "Цвет", "Обивка", "Мощность", "Кол-во дверей", "КПП" },
            connection.Query("SELECT * FROM Model").Select(r =>
                new object?[] { r.ModelCode, r.ModelName, r.Color, r.Upholstery, r.MotorPower, r.DoorCount, r.Transmission }),
            moneyColumns: Array.Empty<int>());

        AddSheet(package, "Связи",
            new[] { "Код поставщика", "Код модели" },
            connection.Query(@"SELECT p.CompanyCode, m.ModelCode FROM Offer of
                               JOIN Producer p ON p.ProducerId = of.ProducerId
                               JOIN Model m ON m.ModelId = of.ModelId").Select(r =>
                new object?[] { r.CompanyCode, r.ModelCode }),
            moneyColumns: Array.Empty<int>());

        AddSheet(package, "Прейскурант",
            new[] { "Код модели", "Год", "Цена (у.е.)", "Подготовка (у.е.)", "Транспорт (у.е.)" },
            connection.Query(@"SELECT m.ModelCode, p.YearOfManufacture, p.Price, p.PrepCost, p.TransportCost
                               FROM PriceList p JOIN Model m ON m.ModelId = p.ModelId").Select(r =>
                new object?[] { r.ModelCode, r.YearOfManufacture, r.Price, r.PrepCost, r.TransportCost }),
            moneyColumns: new[] { 2, 3, 4 });

        AddSheet(package, "Клиенты",
            new[] { "Ф.И.О.", "Телефон", "Адрес" },
            connection.Query("SELECT * FROM Client").Select(r =>
                new object?[] { r.FIO, r.Phone, r.Address }),
            moneyColumns: Array.Empty<int>());

        AddSheet(package, "Продажи",
            new[] { "Договор", "Ф.И.О.", "Модель", "Дата", "Полная стоимость (у.е.)" },
            connection.Query(@"SELECT o.OrderNumber, c.FIO, m.ModelName, o.OrderDate, o.TotalCost
                               FROM [Order] o
                               JOIN Client c ON c.ClientId = o.ClientId
                               JOIN Model m ON m.ModelId = o.ModelId").Select(r =>
                new object?[] { r.OrderNumber, r.FIO, r.ModelName, r.OrderDate, r.TotalCost }),
            moneyColumns: new[] { 4 });

        package.SaveAs(new FileInfo(path));
        return $"Excel-файл сохранён: {path}";
    }

    private static void AddSheet(ExcelPackage package, string name, string[] headers,
        System.Collections.Generic.IEnumerable<object?[]> rows, int[] moneyColumns)
    {
        var sheet = package.Workbook.Worksheets.Add(name);

        for (int col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cells[1, col + 1];
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var r in rows)
        {
            for (int col = 0; col < r.Length; col++)
            {
                var cell = sheet.Cells[row, col + 1];
                cell.Value = r[col];
                if (moneyColumns.Contains(col))
                    cell.Style.Numberformat.Format = "#,##0.00 \"у.е.\"";
            }
            row++;
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    // ---------------- PDF ----------------

    public static string ExportPdf(string path)
    {
        using var connection = Database.GetConnection();

        var suppliers = connection.Query(@"
            SELECT p.CompanyName, p.Phone, p.Email,
                   COUNT(DISTINCT of.ModelId) AS ModelsCount,
                   COUNT(o.OrderId) AS SoldCount
            FROM Producer p
            LEFT JOIN Offer of ON of.ProducerId = p.ProducerId
            LEFT JOIN [Order] o ON o.ModelId = of.ModelId
            GROUP BY p.ProducerId
            ORDER BY p.CompanyName").ToList();

        var models = connection.Query(@"
            SELECT m.ModelName, p.YearOfManufacture,
                   p.Price, p.PrepCost, p.TransportCost,
                   (p.Price + p.PrepCost + p.TransportCost) AS TotalCost,
                   (SELECT COUNT(*) FROM [Order] o WHERE o.ModelId = m.ModelId) AS SoldCount
            FROM Model m
            JOIN PriceList p ON p.ModelId = m.ModelId
            ORDER BY m.ModelName").ToList();

        var monthly = connection.Query<(string Month, double Count)>(@"
            SELECT strftime('%Y-%m', OrderDate) AS Month, COUNT(*) AS Count
            FROM [Order] GROUP BY Month ORDER BY Month").ToList();

        var totalSold = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM [Order]");
        var totalRevenue = connection.ExecuteScalar<decimal?>("SELECT SUM(TotalCost) FROM [Order]") ?? 0;

        var font = CreateBaseFont();
        var titleFont = new Font(font, 16f, Font.BOLD);
        var headerFont = new Font(font, 11f, Font.BOLD);
        var cellFont = new Font(font, 10f);

        using var document = new Document(iTextSharp.text.PageSize.A4.Rotate(), 30f, 30f, 30f, 30f);
        PdfWriter.GetInstance(document, new FileStream(path, FileMode.Create));
        document.Open();

        document.Add(new Paragraph("Аналитический отчёт — Автосалон", titleFont));
        document.Add(new Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", cellFont));
        document.Add(Chunk.Newline);

        // Таблица по поставщикам
        var supplierTable = new PdfPTable(5) { WidthPercentage = 100f };
        AddHeader(supplierTable, headerFont, "Поставщик", "Телефон", "Email", "Моделей предлагает", "Продано");
        foreach (var s in suppliers)
        {
            AddCell(supplierTable, cellFont, (string)s.CompanyName);
            AddCell(supplierTable, cellFont, (string)s.Phone);
            AddCell(supplierTable, cellFont, (string)s.Email);
            AddCell(supplierTable, cellFont, s.ModelsCount.ToString());
            AddCell(supplierTable, cellFont, s.SoldCount.ToString());
        }
        document.Add(supplierTable);
        document.Add(Chunk.Newline);

        // Таблица по моделям
        var modelTable = new PdfPTable(7) { WidthPercentage = 100f };
        AddHeader(modelTable, headerFont, "Модель", "Год", "Цена", "Подготовка", "Транспорт", "Полная стоимость", "Продано");
        foreach (var m in models)
        {
            AddCell(modelTable, cellFont, (string)m.ModelName);
            AddCell(modelTable, cellFont, m.YearOfManufacture.ToString());
            AddCell(modelTable, cellFont, Money((decimal)m.Price));
            AddCell(modelTable, cellFont, Money((decimal)m.PrepCost));
            AddCell(modelTable, cellFont, Money((decimal)m.TransportCost));
            AddCell(modelTable, cellFont, Money((decimal)m.TotalCost));
            AddCell(modelTable, cellFont, m.SoldCount.ToString());
        }
        document.Add(modelTable);
        document.Add(Chunk.Newline);

        // Итоги
        document.Add(new Paragraph($"Всего продаж: {totalSold}", headerFont));
        document.Add(new Paragraph($"Общая выручка: {Money(totalRevenue)} у.е.", headerFont));

        // График динамики продаж по месяцам
        if (monthly.Count > 0)
        {
            var chart = new SKCartesianChart
            {
                Width = 1000,
                Height = 450,
                Series = new LiveChartsCore.ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Name = "Продажи по месяцам",
                        Values = monthly.Select(x => (double)x.Count).ToArray()
                    }
                },
                XAxes = new[] { new Axis { Labels = monthly.Select(x => (string)x.Month).ToArray() } },
                YAxes = new[] { new Axis { Name = "Количество", MinLimit = 0 } }
            };
            using var image = chart.GetImage();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var pdfImage = global::iTextSharp.text.Image.GetInstance(data.ToArray());
            pdfImage.ScaleToFit(document.PageSize.Width - 60f, 350f);
            document.Add(pdfImage);
        }

        document.Close();
        return $"PDF-отчёт сохранён: {path}";
    }

    private static string Money(decimal value) => value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));

    private static BaseFont CreateBaseFont()
    {
        string[] candidates =
        {
            @"C:\Windows\Fonts\arial.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
        };
        var path = candidates.FirstOrDefault(File.Exists);
        return path != null
            ? BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED)
            : BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
    }

    private static void AddHeader(PdfPTable table, Font font, params string[] headers)
    {
        foreach (var h in headers)
            table.AddCell(new PdfPCell(new Phrase(h, font)) { BackgroundColor = new BaseColor(230, 230, 230) });
    }

    private static void AddCell(PdfPTable table, Font font, string text)
        => table.AddCell(new PdfPCell(new Phrase(text, font)));
}
