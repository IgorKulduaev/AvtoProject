using AutoSalesApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AutoSalesApp.Data;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;

namespace AutoSalesApp.Services;

public class ImportResult
{
    public int Imported { get; set; }
    public List<string> Errors { get; } = new();
    public string Summary => $"Импортировано: {Imported}, ошибок: {Errors.Count}";
}

public static class ImportService
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static void SetExcelLicense()
    {
        ExcelPackage.License.SetNonCommercialPersonal("AutoSalesApp");
    }

    // ---------------- CSV ----------------

    /// Импорт CSV (разделитель ';', первая строка — заголовок).
    /// entity: "producers" | "models" | "clients" | "pricelist"
    public static ImportResult ImportCsv(string path, string entity)
    {
        var result = new ImportResult();
        var lines = File.ReadAllLines(path);
        using var db = new AppDbContext();

        for (int i = 1; i < lines.Length; i++) // пропускаем заголовок
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = line.Split(';').Select(c => c.Trim()).ToArray();
            try
            {
                switch (entity)
                {
                    case "producers": ImportProducerCsv(db, cells); break;
                    case "models": ImportModelCsv(db, cells); break;
                    case "clients": ImportClientCsv(db, cells); break;
                    case "pricelist": ImportPriceCsv(db, cells); break;
                }
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Строка {i + 1}: {ex.Message}");
            }
        }
        return result;
    }

    private static void ImportProducerCsv(AppDbContext db, string[] c)
    {
        Require(c, 4, "Код;Название;Телефон;Email;Web-сайт");
        ValidateEmail(c[3]);
        db.Producers.Add(new Producer { CompanyCode = c[0], CompanyName = c[1], Phone = c[2], Email = c[3], Website = c.Length > 4 && c[4] != "" ? c[4] : null });
        db.SaveChanges();
    }

    private static void ImportModelCsv(AppDbContext db, string[] c)
    {
        Require(c, 7, "Код;Наименование;Цвет;Обивка;Мощность;Кол-во дверей;КПП");
        var doors = ParseInt(c[5], "количество дверей");
        if (doors < 2) throw new FormatException("количество дверей должно быть не меньше 2");
        db.Models.Add(new Model { ModelCode = c[0], ModelName = c[1], Color = c[2], Upholstery = c[3], MotorPower = c[4], DoorCount = doors, Transmission = c[6] });
        db.SaveChanges();
    }

    private static void ImportClientCsv(AppDbContext db, string[] c)
    {
        Require(c, 3, "ФИО;Телефон;Адрес");
        db.Clients.Add(new Client { FIO = c[0], Phone = c[1], Address = c[2] });
        db.SaveChanges();
    }

    private static void ImportPriceCsv(AppDbContext db, string[] c)
    {
        Require(c, 5, "Код модели;Год;Цена;Подготовка;Транспорт");
        var modelId = FindModelId(db, c[0]);
        var year = ParseInt(c[1], "год выпуска");
        CheckYear(year);
        var price = ParseDecimal(c[2], "цена");
        var prep = ParseDecimal(c[3], "предпродажная подготовка");
        var transport = ParseDecimal(c[4], "транспортные издержки");
        CheckPrice(price, prep, transport);
        if (db.PriceLists.Any(p => p.ModelId == modelId))
            throw new InvalidOperationException("для модели уже есть прейскурант (1:1)");
        db.PriceLists.Add(new PriceList { ModelId = modelId, YearOfManufacture = year, Price = price, PrepCost = prep, TransportCost = transport });
        db.SaveChanges();
    }

    // ---------------- Excel (XLSX) ----------------

    /// Импорт Excel. entity: "offers" (CompanyCode|ModelCode) | "sales" (OrderNumber|FIO|ModelCode|OrderDate|TotalCost)
    public static ImportResult ImportExcel(string path, string entity)
    {
        SetExcelLicense();
        var result = new ImportResult();
        using var package = new ExcelPackage(new FileInfo(path));
        var sheet = package.Workbook.Worksheets.FirstOrDefault()
                    ?? throw new InvalidOperationException("В файле нет ни одного листа");

        using var db = new AppDbContext();

        for (int row = 2; row <= sheet.Dimension?.End.Row; row++)
        {
            try
            {
                switch (entity)
                {
                    case "offers":
                    {
                        var producerCode = sheet.Cells[row, 1].Text.Trim();
                        var modelCode = sheet.Cells[row, 2].Text.Trim();
                        var producerId = FindProducerId(db, producerCode);
                        var modelId = FindModelId(db, modelCode);
                        db.Offers.Add(new Offer { ProducerId = producerId, ModelId = modelId });
                        db.SaveChanges();
                        break;
                    }
                    case "sales":
                    {
                        var orderNumber = sheet.Cells[row, 1].Text.Trim();
                        var fio = sheet.Cells[row, 2].Text.Trim();
                        var modelCode = sheet.Cells[row, 3].Text.Trim();
                        var dateText = sheet.Cells[row, 4].Text.Trim();
                        var costText = sheet.Cells[row, 5].Text.Trim();

                        if (string.IsNullOrEmpty(orderNumber)) throw new FormatException("пустой номер договора");
                        var clientId = FindClientId(db, fio);
                        var modelId = FindModelId(db, modelCode);
                        if (!DateTime.TryParse(dateText, new CultureInfo("ru-RU"), DateTimeStyles.None, out var date)
                            && !DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                            throw new FormatException($"не распознана дата '{dateText}'");
                        var total = ParseDecimal(costText, "полная стоимость");
                        db.Orders.Add(new Order { OrderNumber = orderNumber, ClientId = clientId, ModelId = modelId, OrderDate = date, TotalCost = total });
                        db.SaveChanges();
                        break;
                    }
                }
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Строка {row}: {ex.Message}");
            }
        }
        return result;
    }

    // ---------------- JSON (полная структура) ----------------

    public static ImportResult ImportJson(string path)
    {
        var result = new ImportResult();
        var root = JObject.Parse(File.ReadAllText(path));
        using var db = new AppDbContext();

        var producerMap = new Dictionary<string, int>();
        var modelMap = new Dictionary<string, int>();

        foreach (var p in root["поставщики"]?.Children() ?? Enumerable.Empty<JToken>())
        {
            try
            {
                var email = p.Value<string>("e_mail") ?? "";
                if (email != "" && !EmailRegex.IsMatch(email)) throw new FormatException($"некорректный email '{email}'");
                var id = InsertProducer(db, p.Value<string>("название_фирмы") ?? "", p.Value<string>("телефон") ?? "",
                    email, p.Value<string>("web_site"));
                producerMap[p.Value<string>("код_фирмы") ?? ""] = id;
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Поставщик: {ex.Message}"); }
        }

        foreach (var m in root["модели"]?.Children() ?? Enumerable.Empty<JToken>())
        {
            try
            {
                var id = InsertModel(db, m.Value<string>("наименование_модели") ?? "", m.Value<string>("цвет") ?? "",
                    m.Value<string>("обивка") ?? "", m.Value<string>("мощность_двигателя") ?? "",
                    (int?)m.Value<int?>("количество_дверей") ?? 0, m.Value<string>("коробка_передач") ?? "");
                modelMap[m.Value<string>("код_модели") ?? ""] = id;
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Модель: {ex.Message}"); }
        }

        // связи поставщик–модель поддерживаем двумя секциями: "связи" — [[код_фирмы, код_модели]]
        foreach (var link in root["связи"]?.Children() ?? Enumerable.Empty<JToken>())
        {
            try
            {
                var pCode = link[0]?.ToString() ?? "";
                var mCode = link[1]?.ToString() ?? "";
                if (!producerMap.ContainsKey(pCode) || !modelMap.ContainsKey(mCode))
                    throw new InvalidOperationException($"не найден внешний ключ ({pCode}, {mCode})");
                db.Offers.Add(new Offer { ProducerId = producerMap[pCode], ModelId = modelMap[mCode] });
                db.SaveChanges();
            }
            catch (Exception ex) { result.Errors.Add($"Связь: {ex.Message}"); }
        }

        foreach (var pr in root["прейскурант"]?.Children() ?? Enumerable.Empty<JToken>())
        {
            try
            {
                var code = pr.Value<string>("код_модели") ?? "";
                if (!modelMap.TryGetValue(code, out var modelId))
                    throw new InvalidOperationException($"не найден код модели '{code}'");
                var year = pr.Value<int?>("год_выпуска") ?? DateTime.Now.Year;
                CheckYear(year);
                var price = pr.Value<decimal?>("цена_уе") ?? 0;
                var prep = pr.Value<decimal?>("предпродажная_подготовка_уе") ?? 0;
                var transport = pr.Value<decimal?>("транспортные_издержки_уе") ?? 0;
                CheckPrice(price, prep, transport);
                db.PriceLists.Add(new PriceList { ModelId = modelId, YearOfManufacture = year, Price = price, PrepCost = prep, TransportCost = transport });
                db.SaveChanges();
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Прейскурант: {ex.Message}"); }
        }

        foreach (var c in root["клиенты"]?.Children() ?? Enumerable.Empty<JToken>())
        {
            try
            {
                var clientId = InsertClient(db, c.Value<string>("фио") ?? "", c.Value<string>("телефон") ?? "",
                    c.Value<string>("адрес") ?? "");
                result.Imported++;

                var orderNumber = c.Value<string>("номер_договора");
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    var code = c.Value<string>("код_модели") ?? "";
                    if (!modelMap.TryGetValue(code, out var modelId))
                    {
                        modelId = TryFindModelId(db, code) ?? throw new InvalidOperationException($"не найден код модели '{code}'");
                    }
                    if (!DateTime.TryParse(c.Value<string>("дата_покупки"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        date = DateTime.Now;
                    var priceItem = db.PriceLists.FirstOrDefault(p => p.ModelId == modelId);
                    var total = (priceItem?.Price + priceItem?.PrepCost + priceItem?.TransportCost) ?? 0m;
                    db.Orders.Add(new Order { OrderNumber = orderNumber, ClientId = clientId, ModelId = modelId, OrderDate = date, TotalCost = total });
                    db.SaveChanges();
                }
            }
            catch (Exception ex) { result.Errors.Add($"Клиент: {ex.Message}"); }
        }

        return result;
    }

    // ---------------- XML (полная структура, атрибуты) ----------------

    public static ImportResult ImportXml(string path)
    {
        var result = new ImportResult();
        var doc = XDocument.Load(path);
        var root = doc.Root ?? throw new InvalidOperationException("пустой XML");
        using var db = new AppDbContext();

        var producerMap = new Dictionary<string, int>();
        var modelMap = new Dictionary<string, int>();

        foreach (var p in root.Element("поставщики")?.Elements("поставщик") ?? Enumerable.Empty<XElement>())
        {
            try
            {
                var email = (string?)p.Attribute("e_mail") ?? "";
                if (email != "" && !EmailRegex.IsMatch(email)) throw new FormatException($"некорректный email '{email}'");
                var id = InsertProducer(db, (string?)p.Attribute("название_фирмы") ?? "",
                    (string?)p.Attribute("телефон") ?? "", email, (string?)p.Attribute("web_site"));
                producerMap[(string?)p.Attribute("код_фирмы") ?? ""] = id;
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Поставщик: {ex.Message}"); }
        }

        foreach (var m in root.Element("модели")?.Elements("модель") ?? Enumerable.Empty<XElement>())
        {
            try
            {
                var doors = (int?)m.Attribute("количество_дверей") ?? 0;
                var id = InsertModel(db, (string?)m.Attribute("наименование_модели") ?? "",
                    (string?)m.Attribute("цвет") ?? "", (string?)m.Attribute("обивка") ?? "",
                    (string?)m.Attribute("мощность_двигателя") ?? "", doors,
                    (string?)m.Attribute("коробка_передач") ?? "");
                modelMap[(string?)m.Attribute("код_модели") ?? ""] = id;
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Модель: {ex.Message}"); }
        }

        foreach (var l in root.Element("связи")?.Elements("связь") ?? Enumerable.Empty<XElement>())
        {
            try
            {
                var pCode = (string?)l.Attribute("код_фирмы") ?? "";
                var mCode = (string?)l.Attribute("код_модели") ?? "";
                if (!producerMap.ContainsKey(pCode) || !modelMap.ContainsKey(mCode))
                    throw new InvalidOperationException($"не найден внешний ключ ({pCode}, {mCode})");
                db.Offers.Add(new Offer { ProducerId = producerMap[pCode], ModelId = modelMap[mCode] });
                db.SaveChanges();
            }
            catch (Exception ex) { result.Errors.Add($"Связь: {ex.Message}"); }
        }

        foreach (var pr in root.Element("прейскурант")?.Elements("прейскурант") ?? Enumerable.Empty<XElement>())
        {
            try
            {
                var code = (string?)pr.Attribute("код_модели") ?? "";
                if (!modelMap.TryGetValue(code, out var modelId))
                    throw new InvalidOperationException($"не найден код модели '{code}'");
                var year = (int?)pr.Attribute("год_выпуска") ?? DateTime.Now.Year;
                CheckYear(year);
                var price = (decimal?)pr.Attribute("цена_уе") ?? 0;
                var prep = (decimal?)pr.Attribute("предпродажная_подготовка_уе") ?? 0;
                var transport = (decimal?)pr.Attribute("транспортные_издержки_уе") ?? 0;
                CheckPrice(price, prep, transport);
                db.PriceLists.Add(new PriceList { ModelId = modelId, YearOfManufacture = year, Price = price, PrepCost = prep, TransportCost = transport });
                db.SaveChanges();
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Прейскурант: {ex.Message}"); }
        }

        foreach (var c in root.Element("клиенты")?.Elements("клиент") ?? Enumerable.Empty<XElement>())
        {
            try
            {
                var clientId = InsertClient(db, (string?)c.Attribute("фио") ?? "",
                    (string?)c.Attribute("телефон") ?? "", (string?)c.Attribute("адрес") ?? "");
                result.Imported++;

                var orderNumber = (string?)c.Attribute("номер_договора");
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    var code = (string?)c.Attribute("код_модели") ?? "";
                    if (!modelMap.TryGetValue(code, out var modelId))
                    {
                        modelId = TryFindModelId(db, code) ?? throw new InvalidOperationException($"не найден код модели '{code}'");
                    }
                    if (!DateTime.TryParse((string?)c.Attribute("дата_покупки"), CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var date))
                        date = DateTime.Now;
                    var priceItem = db.PriceLists.FirstOrDefault(p => p.ModelId == modelId);
                    var total = (priceItem?.Price + priceItem?.PrepCost + priceItem?.TransportCost) ?? 0m;
                    db.Orders.Add(new Order { OrderNumber = orderNumber, ClientId = clientId, ModelId = modelId, OrderDate = date, TotalCost = total });
                    db.SaveChanges();
                }
            }
            catch (Exception ex) { result.Errors.Add($"Клиент: {ex.Message}"); }
        }

        return result;
    }

    // ---------------- Общие помощники ----------------

    private static int InsertProducer(AppDbContext db, string name, string phone, string email, string? site)
    {
        var p = new Producer { CompanyCode = $"auto-{Guid.NewGuid():N}"[..30], CompanyName = name, Phone = phone, Email = email, Website = site };
        db.Producers.Add(p);
        db.SaveChanges();
        return p.ProducerId;
    }

    private static int InsertModel(AppDbContext db, string name, string color, string uph, string power, int doors, string trans)
    {
        if (doors < 2) throw new FormatException("количество дверей должно быть не меньше 2");
        var m = new Model { ModelCode = $"auto-{Guid.NewGuid():N}"[..30], ModelName = name, Color = color, Upholstery = uph, MotorPower = power, DoorCount = doors, Transmission = trans };
        db.Models.Add(m);
        db.SaveChanges();
        return m.ModelId;
    }

    private static int InsertClient(AppDbContext db, string fio, string phone, string address)
    {
        var c = new Client { FIO = fio, Phone = phone, Address = address };
        db.Clients.Add(c);
        db.SaveChanges();
        return c.ClientId;
    }

    private static int FindModelId(AppDbContext db, string code)
    {
        var id = int.TryParse(code, out var num) ? num : 0;
        var m = db.Models.FirstOrDefault(m => m.ModelCode == code || m.ModelId == id)
            ?? throw new InvalidOperationException($"модель с кодом '{code}' не найдена");
        return m.ModelId;
    }

    private static int? TryFindModelId(AppDbContext db, string code)
    {
        var id = int.TryParse(code, out var num) ? num : 0;
        return db.Models.FirstOrDefault(m => m.ModelCode == code || m.ModelId == id)?.ModelId;
    }

    private static int FindProducerId(AppDbContext db, string code)
    {
        var id = int.TryParse(code, out var num) ? num : 0;
        var p = db.Producers.FirstOrDefault(p => p.CompanyCode == code || p.ProducerId == id)
            ?? throw new InvalidOperationException($"поставщик с кодом '{code}' не найден");
        return p.ProducerId;
    }

    private static int FindClientId(AppDbContext db, string fio)
    {
        var id = int.TryParse(fio, out var num) ? num : 0;
        var c = db.Clients.FirstOrDefault(c => c.FIO == fio || c.ClientId == id)
            ?? throw new InvalidOperationException($"клиент '{fio}' не найден");
        return c.ClientId;
    }

    private static void Require(string[] cells, int count, string format)
    {
        if (cells.Length < count || cells.Take(count).Any(string.IsNullOrEmpty))
            throw new FormatException($"ожидается формат: {format}");
    }

    private static void ValidateEmail(string email)
    {
        if (email != "" && !EmailRegex.IsMatch(email))
            throw new FormatException($"некорректный email '{email}'");
    }

    private static int ParseInt(string text, string field)
        => int.TryParse(text, out var v)
            ? v
            : throw new FormatException($"некорректное значение '{text}' для поля «{field}»");

    private static decimal ParseDecimal(string text, string field)
    {
        var normalized = text.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new FormatException($"некорректное значение '{text}' для поля «{field}»");
    }

    private static void CheckYear(int year)
    {
        var max = DateTime.Now.Year + 1;
        if (year > max) throw new FormatException($"год выпуска {year} больше допустимого {max}");
    }

    private static void CheckPrice(decimal price, decimal prep, decimal transport)
    {
        if (price <= 0) throw new FormatException("цена должна быть больше 0");
        if (prep < 0) throw new FormatException("предпродажная подготовка не может быть отрицательной");
        if (transport < 0) throw new FormatException("транспортные издержки не могут быть отрицательными");
    }
}
