using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Npgsql;
using Product.ProductRepository;
using Product.ProductServices;
using System;

var builder = WebApplication.CreateBuilder(args);



var configuration = builder.Configuration;
var host = configuration["POSTGRES_HOST"] ?? throw new InvalidOperationException("POSTGRES_HOST not configured");
var port = configuration["POSTGRES_PORT"] ?? throw new InvalidOperationException("POSTGRES_PORT not configured");
var database = configuration["POSTGRES_DATABASE"] ?? throw new InvalidOperationException("POSTGRES_DATABASE not configured");
var user = configuration["POSTGRES_USER"] ?? throw new InvalidOperationException("POSTGRES_USER not configured");
var password = configuration["POSTGRES_PASSWORD"] ?? throw new InvalidOperationException("POSTGRES_PASSWORD not configured");
var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";

Console.WriteLine("CONNECTION STRING: " + connectionString);
// Register services and dependencies
builder.Services.AddSingleton<NpgsqlDataSource>(new NpgsqlDataSourceBuilder(connectionString).Build());

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Product Service", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service v1"));
}

app.MapGet("/ping", () => {
     Console.WriteLine("Ping endpoint hit!");
     return Results.Ok("Pong!");
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();