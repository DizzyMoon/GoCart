using Npgsql;
using Microsoft.OpenApi.Models;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using product.Messaging.Connection;
using sync_service.Messaging.Connection;
using sync_service.Messaging.Consumers;
using sync_service.ProductServices;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var configuration = builder.Configuration;
var host = configuration["POSTGRES_HOST"] ?? throw new InvalidOperationException("POSTGRES_HOST not configured");
var port = configuration["POSTGRES_PORT"] ?? throw new InvalidOperationException("POSTGRES_PORT not configured");
var database = configuration["POSTGRES_DATABASE"] ?? throw new InvalidOperationException("POSTGRES_DATABASE not configured");
var user = configuration["POSTGRES_USER"] ?? throw new InvalidOperationException("POSTGRES_USER not configured");
var password = configuration["POSTGRES_PASSWORD"] ?? throw new InvalidOperationException("POSTGRES_PASSWORD not configured");
var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";
var esCloudId = configuration["ELASTIC_CLOUD_ID"] ?? throw new InvalidOperationException("ELASTIC_CLOUD_ID not configured");
var esApiKey = configuration["ELASTIC_API_KEY"] ?? throw new InvalidOperationException("ELASTIC_API_KEY not configured");
var settings = new ElasticsearchClientSettings(esCloudId, new ApiKey(esApiKey));
var esClient = new ElasticsearchClient(settings);

builder.Services.AddSingleton(esClient);
builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());

builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IRabbitMqConnectionManager, RabbitMqConnectionManager>();

builder.Services.AddHostedService<AddProductSucceededEventConsumer>();
builder.Services.AddHostedService<AddProductFailedEventConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sync Service API", Version = "v1" });
});

var app = builder.Build();

void ConfigureRabbitMqInfrastructure(IApplicationBuilder webApp)
{
    var logger = webApp.ApplicationServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Sync Serivce: Configuring RabbitMQ infrastructure at startup...");
    try
    {
        using (var serviceScope = webApp.ApplicationServices.CreateScope())
        {
            var rabbitMqManager = serviceScope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

            if (!rabbitMqManager.IsConnected)
            {
                logger.LogInformation("Sync Service: RabbitMQ not connected, attempting to connect...");
                if (rabbitMqManager.TryConnect())
                {
                    logger.LogInformation("Sync Service: Successfully connected to RabbitMQ.");
                    rabbitMqManager.DeclareQueuesAndBindings();
                }
                else
                {
                    logger.LogError(
                        "Sync Service: CRITICAL - Failed to connect to RabbitMQ during startup. Consumers may not start.");
                }
            }
            else
            {
                logger.LogInformation(
                    "Sync Service: Already connected to RabbitMQ. Ensuring queues and bindings are declared.");
                rabbitMqManager.DeclareQueuesAndBindings();
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Order Service: CRITICAL - An unhandled error occurred while configuring RabbitMQ infrastructure during startup.");
    }
}

ConfigureRabbitMqInfrastructure(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => {});
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sync Service API v1"));
}

app.MapGet("/ping", () =>
{
    Console.WriteLine("Ping!");
    return Results.Ok("Pong!");
});

app.MapControllers();
app.UseHttpsRedirection();
app.Run();
