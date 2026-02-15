using InventoryWorker.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Infrastructure.Data;

var builder = Host.CreateApplicationBuilder(args);

// Configuração do DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração do MassTransit com RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Registra o consumer
    x.AddConsumer<PaymentApprovedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
        
        cfg.Host(rabbitConfig["HostName"], "/", h =>
        {
            h.Username(rabbitConfig["UserName"] ?? "guest");
            h.Password(rabbitConfig["Password"] ?? "guest");
        });

        // Configura o endpoint para o consumer
        cfg.ReceiveEndpoint("inventory-queue", e =>
        {
            e.ConfigureConsumer<PaymentApprovedConsumer>(context);
        });
    });
});

// MassTransit 7.x requer registro explícito do HostedService para iniciar o bus
builder.Services.AddMassTransitHostedService(true);

var host = builder.Build();
host.Run();
