using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AutoSalesApp.Data;
using Dapper;
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
        using var connection = Database.GetConnection();
        connection.Open();

        for (int i = 1; i < lines.Length; i++) // пропускаем заголовок
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = line.Split(';').Select(c => c.Trim()).ToArray();
            try
            {
                switch (entity)
                {
                    case "producers": ImportProducerCsv(connection, cells); break;
                    case "models": ImportModelCsv(connection, cells); break;
                    case "clients": ImportClientCsv(connection, cells); break;
                    case "pricelist": ImportPriceCsv(connection, cells); break;
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

    private static void ImportProducerCsv(Microsoft.Data.Sqlite.SqliteConnection db, string[] c)
    {
        Require(c, 4, "Код;Название;Телефон;Email;Web-сайт");
        ValidateEmail(c[3]);
        db.Execute(
            "INSERT INTO Producer (CompanyCode, CompanyName, Phone, Email, Website) VALUES (@code, @name, @phone, @email, @site)",
            new { code = c[0], name = c[1], phone = c[2], email = c[3], site = c.Length > 4 && c[4] != "" ? c[4] : null });
    }

    private static void ImportModelCsv(Microsoft.Data.Sqlite.SqliteConnection db, string[] c)
    {
        Require(c, 7, "Код;Наименование;Цвет;Обивка;Мощность;Кол-во дверей;КПП");
        var doors = ParseInt(c[5], "количество дверей");
        if (doors < 2) throw new FormatException("количество дверей должно быть не меньше 2");
        db.Execute(
            @"INSERT INTO Model (ModelCode, ModelName, Color, Upholstery, MotorPower, DoorCount, Transmission)
              VALUES (@code, @name, @color, @uph, @power, @doors, @trans)",
            new { code = c[0], name = c[1], color = c[2], uph = c[3], power = c[4], doors, trans = c[6] });
    }

    private static void ImportClientCsv(Microsoft.Data.Sqlite.SqliteConnection db, string[] c)
    {
        Require(c, 3, "ФИО;Телефон;Адрес");
        db.Execute("INSERT INTO Client (FIO, Phone, Address) VALUES (@fio, @phone, @address)",
            new { fio = c[0], phone = c[1], address = c[2] });
    }

    private static void ImportPriceCsv(Microsoft.Data.Sqlite.SqliteConnection db, string[] c)
    {
        Require(c, 5, "Код модели;Год;Цена;Подготовка;Транспорт");
        var modelId = FindModelId(db, c[0]);
        var year = ParseInt(c[1], "год выпуска");
        CheckYear(year);
        var price = ParseDecimal(c[2], "цена");
        var prep = ParseDecimal(c[3], "предпродажная подготовка");
        var transport = ParseDecimal(c[4], "транспортные издержки");
        CheckPrice(price, prep, transport);
        if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM PriceList WHERE ModelId = @modelId", new { modelId }) > 0)
            throw new InvalidOperationException("для модели уже есть прейскурант (1:1)");
        db.Execute(
            "INSERT INTO PriceList (ModelId, YearOfManufacture, Price, PrepCost, TransportCost) VALUES (@modelId, @year, @price, @prep, @transport)",
            new { modelId, year, price, prep, transport });
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

        using var connection = Database.GetConnection();
        connection.Open();

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
                        var producerId = FindProducerId(connection, producerCode);
                        var modelId = FindModelId(connection, modelCode);
                        connection.Execute("INSERT INTO Offer (ProducerId, ModelId) VALUES (@producerId, @modelId)",
                            new { producerId, modelId });
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
                        var clientId = FindClientId(connection, fio);
                        var modelId = FindModelId(connection, modelCode);
                        if (!DateTime.TryParse(dateText, new CultureInfo("ru-RU"), DateTimeStyles.None, out var date)
                            && !DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                            throw new FormatException($"не распознана дата '{dateText}'");
                        var total = ParseDecimal(costText, "полная стоимость");
                        connection.Execute(
                            "INSERT INTO [Order] (OrderNumber, ClientId, ModelId, OrderDate, TotalCost) VALUES (@num, @clientId, @modelId, @date, @total)",
                            new { num = orderNumber, clientId, modelId, date, total });
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
        using var connection = Database.GetConnection();
        connection.Open();

        var producerMap = new Dictionary<string, int>();
        var modelMap = new Dictionary<string, int>();

        foreach (var p in root["поставщики"]?.Children() ?? Enumerable.Empty<JToken>())
        {
            try
            {
                var email = p.Value<string>("e_mail") ?? "";
                if (email != "" && !EmailRegex.IsMatch(email)) throw new FormatException($"некорректный email '{email}'");
                var id = InsertProducer(connection, p.Value<string>("название_фирмы") ?? "", p.Value<string>("телефон") ?? "",
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
                var id = InsertModel(connection, m.Value<string>("наименование_модели") ?? "", m.Value<string>("цвет") ?? "",
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
                connection.Execute("INSERT INTO Offer (ProducerId, ModelId) VALUES (@p, @m)",
                    new { p = producerMap[pCode], m = modelMap[mCode] });
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
                connection.Execute(
                    "INSERT INTO PriceList (ModelId, YearOfManufacture, Price, PrepCost, TransportCost) VALUES (@m, @y, @p, @pr, @t)",
                    new { m = modelId, y = year, p = price, pr = prep, t = transport });
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Прейскурант: {ex.Message}"); }
        }

        foreach (var c in root["клиенты"]?.Children() ?? Enumerable.Empty<JToken>())
        {
            try
            {
                var clientId = InsertClient(connection, c.Value<string>("фио") ?? "", c.Value<string>("телефон") ?? "",
                    c.Value<string>("адрес") ?? "");
                result.Imported++;

                var orderNumber = c.Value<string>("номер_договора");
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    var code = c.Value<string>("код_модели") ?? "";
                    if (!modelMap.TryGetValue(code, out var modelId))
                    {
                        modelId = TryFindModelId(connection, code) ?? throw new InvalidOperationException($"не найден код модели '{code}'");
                    }
                    if (!DateTime.TryParse(c.Value<string>("дата_покупки"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        date = DateTime.Now;
                    var total = connection.ExecuteScalar<decimal?>(
                        "SELECT Price + PrepCost + TransportCost FROM PriceList WHERE ModelId = @modelId", new { modelId }) ?? 0;
                    connection.Execute(
                        "INSERT INTO [Order] (OrderNumber, ClientId, ModelId, OrderDate, TotalCost) VALUES (@n, @c, @m, @d, @t)",
                        new { n = orderNumber, c = clientId, m = modelId, d = date, t = total });
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
        using var connection = Database.GetConnection();
        connection.Open();

        var producerMap = new Dictionary<string, int>();
        var modelMap = new Dictionary<string, int>();

        foreach (var p in root.Element("поставщики")?.Elements("поставщик") ?? Enumerable.Empty<XElement>())
        {
            try
            {
                var email = (string?)p.Attribute("e_mail") ?? "";
                if (email != "" && !EmailRegex.IsMatch(email)) throw new FormatException($"некорректный email '{email}'");
                var id = InsertProducer(connection, (string?)p.Attribute("название_фирмы") ?? "",
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
                var id = InsertModel(connection, (string?)m.Attribute("наименование_модели") ?? "",
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
                connection.Execute("INSERT INTO Offer (ProducerId, ModelId) VALUES (@p, @m)",
                    new { p = producerMap[pCode], m = modelMap[mCode] });
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
                connection.Execute(
                    "INSERT INTO PriceList (ModelId, YearOfManufacture, Price, PrepCost, TransportCost) VALUES (@m, @y, @p, @pr, @t)",
                    new { m = modelId, y = year, p = price, pr = prep, t = transport });
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"Прейскурант: {ex.Message}"); }
        }

        foreach (var c in root.Element("клиенты")?.Elements("клиент") ?? Enumerable.Empty<XElement>())
        {
            try
            {
                var clientId = InsertClient(connection, (string?)c.Attribute("фио") ?? "",
                    (string?)c.Attribute("телефон") ?? "", (string?)c.Attribute("адрес") ?? "");
                result.Imported++;

                var orderNumber = (string?)c.Attribute("номер_договора");
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    var code = (string?)c.Attribute("код_модели") ?? "";
                    if (!modelMap.TryGetValue(code, out var modelId))
                    {
                        modelId = TryFindModelId(connection, code) ?? throw new InvalidOperationException($"не найден код модели '{code}'");
                    }
                    if (!DateTime.TryParse((string?)c.Attribute("дата_покупки"), CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var date))
                        date = DateTime.Now;
                    var total = connection.ExecuteScalar<decimal?>(
                        "SELECT Price + PrepCost + TransportCost FROM PriceList WHERE ModelId = @modelId", new { modelId }) ?? 0;
                    connection.Execute(
                        "INSERT INTO [Order] (OrderNumber, ClientId, ModelId, OrderDate, TotalCost) VALUES (@n, @c, @m, @d, @t)",
                        new { n = orderNumber, c = clientId, m = modelId, d = date, t = total });
                }
            }
            catch (Exception ex) { result.Errors.Add($"Клиент: {ex.Message}"); }
        }

        return result;
    }

    // ---------------- Общие помощники ----------------

    private static int InsertProducer(Microsoft.Data.Sqlite.SqliteConnection db, string name, string phone, string email, string? site)
        => db.ExecuteScalar<int>(
            "INSERT INTO Producer (CompanyCode, CompanyName, Phone, Email, Website) VALUES (@code, @name, @phone, @email, @site); SELECT last_insert_rowid()",
            new { code = $"auto-{Guid.NewGuid():N}", name, phone, email, site });

    private static int InsertModel(Microsoft.Data.Sqlite.SqliteConnection db, string name, string color, string uph, string power, int doors, string trans)
    {
        if (doors < 2) throw new FormatException("количество дверей должно быть не меньше 2");
        return db.ExecuteScalar<int>(
            @"INSERT INTO Model (ModelCode, ModelName, Color, Upholstery, MotorPower, DoorCount, Transmission)
              VALUES (@code, @name, @color, @uph, @power, @doors, @trans); SELECT last_insert_rowid()",
            new { code = $"auto-{Guid.NewGuid():N}", name, color, uph, power, doors, trans });
    }

    private static int InsertClient(Microsoft.Data.Sqlite.SqliteConnection db, string fio, string phone, string address)
        => db.ExecuteScalar<int>(
            "INSERT INTO Client (FIO, Phone, Address) VALUES (@fio, @phone, @address); SELECT last_insert_rowid()",
            new { fio, phone, address });

    private static int FindModelId(Microsoft.Data.Sqlite.SqliteConnection db, string code)
        => TryFindModelId(db, code) ?? throw new InvalidOperationException($"модель с кодом '{code}' не найдена");

    private static int? TryFindModelId(Microsoft.Data.Sqlite.SqliteConnection db, string code)
        => db.ExecuteScalar<int?>("SELECT ModelId FROM Model WHERE ModelCode = @code OR CAST(ModelId AS TEXT) = @code",
            new { code });

    private static int FindProducerId(Microsoft.Data.Sqlite.SqliteConnection db, string code)
        => db.ExecuteScalar<int?>("SELECT ProducerId FROM Producer WHERE CompanyCode = @code OR CAST(ProducerId AS TEXT) = @code",
            new { code }) ?? throw new InvalidOperationException($"поставщик с кодом '{code}' не найден");

    private static int FindClientId(Microsoft.Data.Sqlite.SqliteConnection db, string fio)
        => db.ExecuteScalar<int?>("SELECT ClientId FROM Client WHERE FIO = @fio OR CAST(ClientId AS TEXT) = @fio",
            new { fio }) ?? throw new InvalidOperationException($"клиент '{fio}' не найден");

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
