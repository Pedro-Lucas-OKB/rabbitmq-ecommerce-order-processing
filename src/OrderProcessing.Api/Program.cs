using System.Text.Json.Serialization;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderProcessing.Api.Endpoints;
using OrderProcessing.Core.Validators;
using OrderProcessing.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "E-Commerce Order Processing API",
        Version = "v1",
        Description = "API para sistema de processamento de pedidos de e-commerce com MassTransit/RabbitMQ.",
        Contact = new OpenApiContact
        {
            Name = "Pedro Lucas Dev",
            Email = "pedrolucasep5100@gmail.com",
            Url = new Uri("https://github.com/Pedro-Lucas-OKB")
        },
    });
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração do MassTransit com RabbitMQ
// Desabilitado quando DISABLE_MASSTRANSIT=true (usado em testes de integração)
if (!string.Equals(builder.Configuration["DISABLE_MASSTRANSIT"], "true", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddMassTransit(config =>
    {
        config.UsingRabbitMq((context, cfg) =>
        {
            var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
            
            cfg.Host(rabbitConfig["HostName"] ?? "localhost", "/", h =>
            {
                h.Username(rabbitConfig["UserName"] ?? "guest");
                h.Password(rabbitConfig["Password"] ?? "guest");
            });
        });
    });

    // MassTransit 7.x requer registro explícito do HostedService para iniciar o bus
    builder.Services.AddMassTransitHostedService(true);
}

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapOrderEndpoints();

app.UseHttpsRedirection();

app.Run();
