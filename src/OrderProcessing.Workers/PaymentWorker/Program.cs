using Microsoft.EntityFrameworkCore;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Messaging;
using PaymentWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração do RabbitMQ
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
