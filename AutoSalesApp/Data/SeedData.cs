using System;
using AutoSalesApp.Models;
using Dapper;

namespace AutoSalesApp.Data
{
    public static class SeedData
    {
        public static void Insert()
        {
            using var connection = Database.GetConnection();
            connection.Open();

            // Проверяем, есть ли уже данные
            var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Producer");
            if (count > 0) return; // уже заполнено

            // Поставщики (минимум 5)
            var producers = new[]
            {
                new Producer { CompanyCode = "AVP001", CompanyName = "АвтоПлюс", Phone = "8-495-123-45-67", Email = "info@avtoplus.ru", Website = "www.avtoplus.ru" },
                new Producer { CompanyCode = "MOT002", CompanyName = "МоторСити", Phone = "8-495-234-56-78", Email = "sales@motorsity.ru", Website = "www.motorsity.ru" },
                new Producer { CompanyCode = "DRV003", CompanyName = "ДрайвАвто", Phone = "8-495-345-67-89", Email = "hello@driveauto.ru", Website = null },
                new Producer { CompanyCode = "SPD004", CompanyName = "СпидМоторс", Phone = "8-495-456-78-90", Email = "info@speedmotors.ru", Website = "www.speedmotors.ru" },
                new Producer { CompanyCode = "GLD005", CompanyName = "ГолдКар", Phone = "8-495-567-89-01", Email = "contact@goldcar.ru", Website = null }
            };
            foreach (var p in producers)
            {
                connection.Execute(@"
                    INSERT INTO Producer (CompanyCode, CompanyName, Phone, Email, Website)
                    VALUES (@CompanyCode, @CompanyName, @Phone, @Email, @Website)", p);
            }

            // Модели (минимум 6)
            var models = new[]
            {
                new Model { ModelCode = "CAM001", ModelName = "Toyota Camry", Color = "Чёрный", Upholstery = "Кожа", MotorPower = "150/200 кВт", DoorCount = 4, Transmission = "автоматическая" },
                new Model { ModelCode = "COR002", ModelName = "Toyota Corolla", Color = "Белый", Upholstery = "Ткань", MotorPower = "90/120 кВт", DoorCount = 4, Transmission = "механическая" },
                new Model { ModelCode = "BMW003", ModelName = "BMW X5", Color = "Синий", Upholstery = "Кожа", MotorPower = "200/300 кВт", DoorCount = 5, Transmission = "автоматическая" },
                new Model { ModelCode = "AUD004", ModelName = "Audi A6", Color = "Серый", Upholstery = "Кожа", MotorPower = "180/250 кВт", DoorCount = 4, Transmission = "автоматическая" },
                new Model { ModelCode = "KIA005", ModelName = "Kia Rio", Color = "Красный", Upholstery = "Ткань", MotorPower = "74/100 кВт", DoorCount = 4, Transmission = "механическая" },
                new Model { ModelCode = "HYU006", ModelName = "Hyundai Tucson", Color = "Белый", Upholstery = "Кожа", MotorPower = "130/180 кВт", DoorCount = 5, Transmission = "автоматическая" }
            };
            foreach (var m in models)
            {
                connection.Execute(@"
                    INSERT INTO Model (ModelCode, ModelName, Color, Upholstery, MotorPower, DoorCount, Transmission)
                    VALUES (@ModelCode, @ModelName, @Color, @Upholstery, @MotorPower, @DoorCount, @Transmission)", m);
            }

            // Связи Offer (M:N)
            var offers = new[]
            {
                new Offer { ProducerId = 1, ModelId = 1 },
                new Offer { ProducerId = 1, ModelId = 2 },
                new Offer { ProducerId = 2, ModelId = 3 },
                new Offer { ProducerId = 2, ModelId = 4 },
                new Offer { ProducerId = 3, ModelId = 5 },
                new Offer { ProducerId = 4, ModelId = 6 },
                new Offer { ProducerId = 5, ModelId = 1 },
                new Offer { ProducerId = 5, ModelId = 3 }
            };
            foreach (var o in offers)
            {
                connection.Execute("INSERT INTO Offer (ProducerId, ModelId) VALUES (@ProducerId, @ModelId)", o);
            }

            // Прейскурант (минимум 6 записей, 1:1 с Model)
            var prices = new[]
            {
                new PriceList { ModelId = 1, YearOfManufacture = 2025, Price = 35000, PrepCost = 500, TransportCost = 800 },
                new PriceList { ModelId = 2, YearOfManufacture = 2025, Price = 22000, PrepCost = 300, TransportCost = 500 },
                new PriceList { ModelId = 3, YearOfManufacture = 2024, Price = 75000, PrepCost = 1000, TransportCost = 1500 },
                new PriceList { ModelId = 4, YearOfManufacture = 2025, Price = 55000, PrepCost = 700, TransportCost = 1000 },
                new PriceList { ModelId = 5, YearOfManufacture = 2025, Price = 15000, PrepCost = 200, TransportCost = 400 },
                new PriceList { ModelId = 6, YearOfManufacture = 2024, Price = 28000, PrepCost = 400, TransportCost = 600 }
            };
            foreach (var p in prices)
            {
                connection.Execute(@"
                    INSERT INTO PriceList (ModelId, YearOfManufacture, Price, PrepCost, TransportCost)
                    VALUES (@ModelId, @YearOfManufacture, @Price, @PrepCost, @TransportCost)", p);
            }

            // Клиенты (минимум 5)
            var clients = new[]
            {
                new Client { FIO = "Иванов Иван Иванович", Phone = "8-495-111-22-33", Address = "Москва" },
                new Client { FIO = "Петров Петр Петрович", Phone = "8-495-222-33-44", Address = "Санкт-Петербург" },
                new Client { FIO = "Сидорова Анна Сергеевна", Phone = "8-495-333-44-55", Address = "Казань" },
                new Client { FIO = "Кузнецов Дмитрий Алексеевич", Phone = "8-495-444-55-66", Address = "Новосибирск" },
                new Client { FIO = "Смирнова Елена Владимировна", Phone = "8-495-555-66-77", Address = "Екатеринбург" }
            };
            foreach (var c in clients)
            {
                connection.Execute(@"
                    INSERT INTO Client (FIO, Phone, Address)
                    VALUES (@FIO, @Phone, @Address)", c);
            }

            // Заказы (продажи)
            var orders = new[]
            {
                new Order { OrderNumber = "Д001", ClientId = 1, ModelId = 1, OrderDate = new DateTime(2025, 6, 1), TotalCost = 36300 },
                new Order { OrderNumber = "Д002", ClientId = 2, ModelId = 3, OrderDate = new DateTime(2025, 6, 15), TotalCost = 77500 },
                new Order { OrderNumber = "Д003", ClientId = 3, ModelId = 2, OrderDate = new DateTime(2025, 7, 1), TotalCost = 22800 },
                new Order { OrderNumber = "Д004", ClientId = 4, ModelId = 5, OrderDate = new DateTime(2025, 7, 10), TotalCost = 15600 },
                new Order { OrderNumber = "Д005", ClientId = 5, ModelId = 6, OrderDate = new DateTime(2025, 8, 1), TotalCost = 29000 }
            };
            foreach (var o in orders)
            {
                connection.Execute(@"
                    INSERT INTO [Order] (OrderNumber, ClientId, ModelId, OrderDate, TotalCost)
                    VALUES (@OrderNumber, @ClientId, @ModelId, @OrderDate, @TotalCost)", o);
            }
        }
    }
}