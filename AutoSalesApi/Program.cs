using AutoSalesApi.Models.Data;
using AutoSalesApi.Models.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions
{
    TrackStatistics = true,
    SizeLimit = 50
}));

builder.Services.AddDbContext<AutoSalesDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ProducerService>();
builder.Services.AddScoped<ModelService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<PriceListService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddCors();
builder.Services.AddResponseCaching();

var app = builder.Build();

// Создаём схему SQLite и наполняем демонстрационными данными при первом запуске.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AutoSalesDbContext>();
    db.Database.EnsureCreated();
    SeedData.Insert(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseResponseCaching();
app.MapControllers();

app.Run();
