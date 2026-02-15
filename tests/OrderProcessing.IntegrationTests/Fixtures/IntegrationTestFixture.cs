using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderProcessing.Api;
using OrderProcessing.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Fixtures;

/// <summary>
/// Fixture de testes de integração que orquestra toda a infraestrutura necessária.
/// 
/// Esta classe é responsável por:
/// - Iniciar container Docker (PostgreSQL) via Testcontainers
/// - Criar uma instância da API em memória via WebApplicationFactory
/// - Usar MassTransit InMemory para mensageria (sem RabbitMQ real)
/// - Aplicar migrations do EF Core no banco de teste
/// - Fornecer um HttpClient pré-configurado para os testes
/// - Limpar todos os recursos após os testes
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    
    public WebApplicationFactory<IApiMarker> Factory { get; private set; } = null!;
    public ApiClient ApiClient { get; private set; } = null!;
    
    private HttpClient _httpClient = null!;
    private DbConnection _dbConnection = null!;
    private DbTransaction _transaction = null!;

    public IntegrationTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithImage("postgres:16-alpine")
            .WithDatabase("ecommerce-testsdb")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithCleanUp(true)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        
        Factory = new WebApplicationFactory<IApiMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                
                // Desabilita o MassTransit real (RabbitMQ) configurado na API
                builder.UseSetting("DISABLE_MASSTRANSIT", "true");

                builder.ConfigureTestServices(services =>
                {
                    // Remove o DbContext original
                    services.RemoveAll<DbContextOptions<AppDbContext>>();

                    // Adiciona DbContext apontando para o container PostgreSQL
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });

                    // Usa MassTransit InMemory para testes (sem RabbitMQ real)
                    // Isso fornece IPublishEndpoint, IBus, etc. funcionais
                    services.AddMassTransit(config =>
                    {
                        config.UsingInMemory((context, cfg) =>
                        {
                            cfg.ConfigureEndpoints(context);
                        });
                    });
                    
                    // MassTransit 7.x requer registro explícito do HostedService
                    services.AddMassTransitHostedService(true);
                });
            });
        
        _httpClient = Factory.CreateClient();
        ApiClient = new ApiClient(_httpClient, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });
        
        var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
        _dbConnection = dbContext.Database.GetDbConnection();
        await _dbConnection.OpenAsync();
        _transaction = await _dbConnection.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        if (_transaction is not null) await _transaction.RollbackAsync();
        if (_dbConnection is not null) await _dbConnection.DisposeAsync();
        
        ApiClient?.HttpClient.Dispose();
        
        // MassTransit 7.x pode lançar ChannelClosedException durante o dispose
        // do InMemory transport. Isso é um bug conhecido e não afeta os testes.
        try
        {
            if (Factory is not null) await Factory.DisposeAsync();
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            // Ignorar - bug conhecido no MassTransit 7.x InMemory transport
        }
        
        await _postgresContainer.DisposeAsync();
    }
}
