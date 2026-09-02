using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Data.Sqlite;
using Dapper;

namespace AutoSalesApp.Data;

public static class Database
{
    private static readonly string ConnectionString = "Data Source=autosales.db";

    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConnectionString);
    }

    public static void Init()
    {
        using var connection = GetConnection();
        connection.Open();

        // Producer
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Producer (
                ProducerId INTEGER PRIMARY KEY AUTOINCREMENT,
                CompanyCode TEXT NOT NULL,
                CompanyName TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Email TEXT NOT NULL,
                Website TEXT
            )
        ");

        // Model
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Model (
                ModelId INTEGER PRIMARY KEY AUTOINCREMENT,
                ModelCode TEXT NOT NULL,
                ModelName TEXT NOT NULL,
                Color TEXT NOT NULL,
                Upholstery TEXT NOT NULL,
                MotorPower TEXT NOT NULL,
                DoorCount INTEGER NOT NULL CHECK(DoorCount >= 2),
                Transmission TEXT NOT NULL
            )
        ");

        // Offer (M:N)
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Offer (
                OfferId INTEGER PRIMARY KEY AUTOINCREMENT,
                ProducerId INTEGER NOT NULL,
                ModelId INTEGER NOT NULL,
                FOREIGN KEY (ProducerId) REFERENCES Producer(ProducerId) ON DELETE RESTRICT,
                FOREIGN KEY (ModelId) REFERENCES Model(ModelId) ON DELETE RESTRICT
            )
        ");

        // PriceList (1:1 с Model)
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS PriceList (
                PriceId INTEGER PRIMARY KEY AUTOINCREMENT,
                ModelId INTEGER NOT NULL UNIQUE,
                YearOfManufacture INTEGER NOT NULL,
                Price REAL NOT NULL CHECK(Price > 0),
                PrepCost REAL NOT NULL CHECK(PrepCost >= 0),
                TransportCost REAL NOT NULL CHECK(TransportCost >= 0),
                FOREIGN KEY (ModelId) REFERENCES Model(ModelId) ON DELETE RESTRICT
            )
        ");

        // Client
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Client (
                ClientId INTEGER PRIMARY KEY AUTOINCREMENT,
                FIO TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Address TEXT NOT NULL
            )
        ");

        // Order (продажа)
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS [Order] (
                OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderNumber TEXT NOT NULL,
                ClientId INTEGER NOT NULL,
                ModelId INTEGER NOT NULL,
                OrderDate TEXT NOT NULL,
                TotalCost REAL NOT NULL,
                FOREIGN KEY (ClientId) REFERENCES Client(ClientId) ON DELETE RESTRICT,
                FOREIGN KEY (ModelId) REFERENCES Model(ModelId) ON DELETE RESTRICT
            )
        ");
    }
}