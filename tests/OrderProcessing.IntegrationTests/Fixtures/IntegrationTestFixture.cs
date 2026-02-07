using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
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

/// <summary>
/// Fixture de testes de integração que orquestra toda a infraestrutura necessária.
/// 
/// Esta classe é responsável por:
/// - Iniciar containers Docker (PostgreSQL e RabbitMQ) via Testcontainers
/// - Criar uma instância da API em memória via WebApplicationFactory
/// - Substituir as configurações reais pelas dos containers de teste
/// - Aplicar migrations do EF Core no banco de teste
/// - Fornecer um HttpClient pré-configurado para os testes
/// - Limpar todos os recursos após os testes
/// 
/// Uso:
/// <code>
/// public class MeuTeste : IClassFixture&lt;IntegrationTestFixture&gt;
/// {
///     public MeuTeste(IntegrationTestFixture fixture)
///     {
///         _client = fixture.ApiClient;
///     }
/// }
/// </code>
/// 
/// A interface IAsyncLifetime do xUnit fornece:
/// - InitializeAsync(): Executado ANTES de qualquer teste da classe
/// - DisposeAsync(): Executado DEPOIS de todos os testes da classe
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    // ╔════════════════════════════════════════════════════════════════════════╗
    // ║                           CONTAINERS DOCKER                            ║
    // ║  Gerenciados pelo Testcontainers - instâncias reais, não mocks!        ║
    // ╚════════════════════════════════════════════════════════════════════════╝
    
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    
    // ╔════════════════════════════════════════════════════════════════════════╗
    // ║                         PROPRIEDADES PÚBLICAS                          ║
    // ║  Expostas para uso nos testes                                          ║
    // ╚════════════════════════════════════════════════════════════════════════╝
    
    /// <summary>
    /// Factory que hospeda a API em memória. Use para acessar serviços via DI.
    /// Exemplo: Factory.Services.CreateScope().ServiceProvider.GetRequiredService&lt;AppDbContext&gt;()
    /// </summary>
    public WebApplicationFactory<IApiMarker> Factory { get; private set; } = null!;
    
    /// <summary>
    /// Cliente HTTP pré-configurado com JsonSerializerOptions corretas.
    /// Use este cliente para fazer requisições à API nos testes.
    /// </summary>
    public ApiClient ApiClient { get; private set; } = null!;
    
    // ╔════════════════════════════════════════════════════════════════════════╗
    // ║                         CAMPOS PRIVADOS                                ║
    // ║  Gerenciamento interno de conexão e transação                          ║
    // ╚════════════════════════════════════════════════════════════════════════╝
    
    private HttpClient _httpClient = null!;
    private DbConnection _dbConnection = null!;
    private DbTransaction _transaction = null!;

    /// <summary>
    /// Construtor: Apenas CONFIGURA os containers, mas NÃO os inicia.
    /// Os containers só serão iniciados no InitializeAsync().
    /// 
    /// IMPORTANTE: Não tente acessar GetConnectionString() ou GetMappedPublicPort() aqui,
    /// pois os containers ainda não estão rodando!
    /// </summary>
    public IntegrationTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithImage("postgres:16-alpine")
            .WithDatabase("ecommerce-testsdb")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithCleanUp(true) // Remove o container automaticamente após os testes
            .Build();
        
        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management")
            .WithImage("rabbitmq:3-management")
            .WithPortBinding(5672, true) // true = porta aleatória no host
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();
    }
    
    /// <summary>
    /// Executado pelo xUnit ANTES de qualquer teste da classe.
    /// Aqui iniciamos os containers e configuramos toda a infraestrutura.
    /// 
    /// Ordem de execução:
    /// 1. Inicia containers Docker (PostgreSQL + RabbitMQ)
    /// 2. Cria WebApplicationFactory com serviços substituídos
    /// 3. Cria HttpClient e ApiClient
    /// 4. Aplica migrations do EF Core
    /// 5. Inicia transação para isolamento dos testes
    /// </summary>
    public async Task InitializeAsync()
    {
        // ┌──────────────────────────────────────────────────────────────────┐
        // │ PASSO 1: Iniciar containers em paralelo para maior performance   │
        // └──────────────────────────────────────────────────────────────────┘
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());
        
        // ┌──────────────────────────────────────────────────────────────────┐
        // │ PASSO 2: Criar a WebApplicationFactory                           │
        // │                                                                  │
        // │ A Factory cria uma versão "in-memory" da nossa API.              │
        // │ Usamos ConfigureTestServices para SUBSTITUIR os serviços reais   │
        // │ pelos serviços que apontam para nossos containers de teste.      │
        // │                                                                  │
        // │ IMPORTANTE: Só podemos fazer isso DEPOIS que os containers       │
        // │ estão rodando, pois precisamos de GetConnectionString() e        │
        // │ GetMappedPublicPort() que só funcionam com containers ativos.    │
        // └──────────────────────────────────────────────────────────────────┘
        Factory = new WebApplicationFactory<IApiMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureTestServices(services =>
                {
                    // Remove o DbContext original (que aponta para o banco de desenvolvimento)
                    services.RemoveAll<DbContextOptions<AppDbContext>>();

                    // Adiciona um novo DbContext apontando para o container PostgreSQL
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });

                    // Remove as configurações originais do RabbitMQ
                    services.RemoveAll<IRabbitMqPublisher>();
                    services.RemoveAll<RabbitMqSettings>();

                    // Cria novas configurações apontando para o container RabbitMQ
                    var rabbitMqSetting = new RabbitMqSettings()
                    {
                        HostName = _rabbitMqContainer.Hostname,
                        Port = _rabbitMqContainer.GetMappedPublicPort(5672), // Porta mapeada aleatoriamente
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
        
        // ┌──────────────────────────────────────────────────────────────────┐
        // │ PASSO 3: Criar o cliente HTTP com opções de serialização         │
        // │                                                                  │
        // │ O ApiClient encapsula o HttpClient e as JsonSerializerOptions    │
        // │ para garantir que enums sejam serializados como strings.         │
        // └──────────────────────────────────────────────────────────────────┘
        _httpClient = Factory.CreateClient();
        ApiClient = new ApiClient(_httpClient, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });
        
        // ┌──────────────────────────────────────────────────────────────────┐
        // │ PASSO 4: Aplicar migrations e preparar o banco de dados          │
        // │                                                                  │
        // │ MigrateAsync() cria todas as tabelas no banco de teste.          │
        // │ A transação garante isolamento: cada teste roda em uma           │
        // │ transação que é revertida (rollback) no final, mantendo o        │
        // │ banco limpo para o próximo teste.                                │
        // └──────────────────────────────────────────────────────────────────┘
        var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
        _dbConnection = dbContext.Database.GetDbConnection();
        await _dbConnection.OpenAsync();
        _transaction = await _dbConnection.BeginTransactionAsync();
    }

    /// <summary>
    /// Executado pelo xUnit DEPOIS de todos os testes da classe.
    /// Limpa todos os recursos na ordem correta (inversa da criação).
    /// 
    /// IMPORTANTE: A ordem de descarte importa!
    /// 1. Primeiro: rollback da transação (desfaz alterações no banco)
    /// 2. Depois: fecha conexão com o banco
    /// 3. Depois: descarta o cliente HTTP
    /// 4. Depois: descarta a Factory (que pode ter conexões abertas)
    /// 5. Por último: para os containers Docker
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_transaction is not null) await _transaction.RollbackAsync();
        if (_dbConnection is not null) await _dbConnection.DisposeAsync();
        
        ApiClient?.HttpClient.Dispose();
        
        if (Factory is not null) await Factory.DisposeAsync();
        
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}
