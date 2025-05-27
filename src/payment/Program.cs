using Microsoft.OpenApi.Models;
using payment.Messaging.Connection;
using payment.Messaging.Publishers;
using payment.PaymentServices;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var configuration = builder.Configuration;

var stripeSecretKey = configuration["STRIPE_SECRET_KEY"] ??
                      throw new InvalidOperationException("Stripe secret key not configured");

StripeConfiguration.ApiKey = stripeSecretKey;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddScoped<IRabbitMqConnectionManager, RabbitMqConnectionManager>();

builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();

builder.Services.AddScoped<PaymentIntentService>();
builder.Services.AddScoped<PaymentMethodService>();

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payment Service API", Version = "v1" });
});

var app = builder.Build();

var rabbitMqManager = app.Services.GetRequiredService<IRabbitMqConnectionManager>();
if (!rabbitMqManager.IsConnected)
{
    rabbitMqManager.TryConnect();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1"));
}

app.MapGet("/ping", () => {
    Console.WriteLine("Ping endpoint hit!");
    return Results.Ok("Pong!");
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();