using Microsoft.OpenApi.Models;
using Npgsql;
using Order.OrderRepository;
using order.OrderService;
using Order.OrderService;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


var configuration = builder.Configuration;
string host = configuration["POSTGRES_HOST"] ?? throw new InvalidOperationException("POSTGRES_HOST not configured");
string port = configuration["POSTGRES_PORT"] ?? throw new InvalidOperationException("POSTGRES_PORT not configured");
string database = configuration["POSTGRES_DATABASE"] ?? throw new InvalidOperationException("POSTGRES_DATABASE not configured");
string user = configuration["POSTGRES_USER"] ?? throw new InvalidOperationException("POSTGRES_USER not configured");
string password = configuration["POSTGRES_PASSWORD"] ?? throw new InvalidOperationException("POSTGRES_PASSWORD not configured");
string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";


builder.Services.AddSingleton<NpgsqlDataSource>(new NpgsqlDataSourceBuilder(connectionString).Build());

Console.WriteLine($"--- Database Configuration ---");
Console.WriteLine($" Target: Host={host}, Port={port}, Database={database}, User={user}");
Console.WriteLine($" NpgsqlDataSource registered.");
Console.WriteLine($"--- End Database Config ---");


builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Service API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1"));
}

app.MapGet("/ping", () => {
     Console.WriteLine("Ping endpoint hit!");
     return Results.Ok("Pong!");
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();