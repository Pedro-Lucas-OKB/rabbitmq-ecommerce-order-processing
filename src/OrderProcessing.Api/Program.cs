using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderProcessing.Api.Endpoints;
using OrderProcessing.Core.Validators;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Messaging;

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
        Description = "API para sistema de processamento de pedidos de e-commerce com RabbitMQ.",
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

// Configuração do RabbitMQ
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

// Registra o Publisher como Singleton (reutiliza conexão)
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

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
