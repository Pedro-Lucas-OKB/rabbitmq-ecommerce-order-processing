using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrderProcessing.Api;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Messaging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly WebApplicationFactory<IApiMarker> _factory;
    
    private DbConnection _dbConnection = null!;
    private DbTransaction _transaction = null!;
    
    public HttpClient Client { get; }

    public IntegrationTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithImage("postgres:16-alpine")
            .WithDatabase("ecommerce-testsdb")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithCleanUp(true)
            .Build();
        
        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management")
            .WithImage("rabbitmq:3-management")
            .WithPortBinding(56721, true)
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();

        _factory = new WebApplicationFactory<IApiMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureTestServices(services =>
                {
                    // Removendo DbContext original
                    services.RemoveAll<DbContextOptions<AppDbContext>>();

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });

                    services.RemoveAll<IRabbitMqPublisher>();
                    services.RemoveAll<RabbitMqSettings>();

                    var rabbitMqSetting = new RabbitMqSettings()
                    {
                        HostName = _rabbitMqContainer.Hostname,
                        Port = _rabbitMqContainer.GetMappedPublicPort(5672),
                        UserName = "guest",
                        Password = "guest",
                        ExchangeName = "order-created-exchange-test",
                        PaymentQueueName = "payment-queue-test",
                        OrderCreatedRoutingKey = "order.created.test"
                    };

                    services.AddSingleton(Options.Create(rabbitMqSetting));
                    services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
                });
            });
        
        Client = _factory.CreateClient();
    }
    
    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());
        
        
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
        _dbConnection = dbContext.Database.GetDbConnection();
        _transaction = await _dbConnection.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        // Limpeza: reverter a transação e parar os contêineres
        await _transaction.RollbackAsync();
        await _dbConnection.DisposeAsync();
        Client.Dispose();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await _factory.DisposeAsync();
    }
}