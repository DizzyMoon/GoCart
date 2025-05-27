using Microsoft.OpenApi.Models;
using Npgsql;
using order.Messaging.Connection;
using order.Messaging.Consumers;
using Order.OrderRepository;
using order.OrderService;
using Order.OrderService;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


var configuration = builder.Configuration;
var host = configuration["POSTGRES_HOST"] ?? throw new InvalidOperationException("POSTGRES_HOST not configured");
var port = configuration["POSTGRES_PORT"] ?? throw new InvalidOperationException("POSTGRES_PORT not configured");
var database = configuration["POSTGRES_DATABASE"] ?? throw new InvalidOperationException("POSTGRES_DATABASE not configured");
var user = configuration["POSTGRES_USER"] ?? throw new InvalidOperationException("POSTGRES_USER not configured");
var password = configuration["POSTGRES_PASSWORD"] ?? throw new InvalidOperationException("POSTGRES_PASSWORD not configured");
var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";


builder.Services.AddSingleton<NpgsqlDataSource>(new NpgsqlDataSourceBuilder(connectionString).Build());

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IRabbitMqConnectionManager, RabbitMqConnectionManager>();

builder.Services.AddHostedService<PaymentSucceededEventConsumer>();
builder.Services.AddHostedService<PaymentFailedEventConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Service API", Version = "v1" });
});

var app = builder.Build();

void ConfigureRabbitMqInfrastructure(IApplicationBuilder webApp)
{
    var logger = webApp.ApplicationServices.GetRequiredService<ILogger<Program>>(); // Or a specific logger for startup
    logger.LogInformation("Order Service: Configuring RabbitMQ infrastructure at startup...");
    try
    {
        using (var serviceScope = webApp.ApplicationServices.CreateScope())
        {
            var rabbitMqManager = serviceScope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();
            
            if (!rabbitMqManager.IsConnected)
            {
                logger.LogInformation("Order Service: RabbitMQ not connected, attempting to connect...");
                if (rabbitMqManager.TryConnect()) // TryConnect should be synchronous or handled appropriately if async
                {
                    logger.LogInformation("Order Service: Successfully connected to RabbitMQ.");
                    rabbitMqManager.DeclareQueuesAndBindings(); // Declare after successful connection
                }
                else
                {
                    logger.LogError("Order Service: CRITICAL - Failed to connect to RabbitMQ during startup. Consumers may not start.");
                    // Consider throwing an exception here if RabbitMQ is essential for startup
                    // throw new Exception("Failed to connect to RabbitMQ during startup.");
                }
            }
            else
            {
                logger.LogInformation("Order Service: Already connected to RabbitMQ. Ensuring queues and bindings are declared.");
                rabbitMqManager.DeclareQueuesAndBindings(); // Ensure declarations if already connected
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Order Service: CRITICAL - An unhandled error occurred while configuring RabbitMQ infrastructure during startup.");
        // Depending on policy, you might want to re-throw to prevent the app from starting in a bad state.
        // throw; 
    }
}

ConfigureRabbitMqInfrastructure(app);

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