using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderProcessing.Core.Entities;
using OrderProcessing.Core.Enums;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    private IConnection? _connection;
    private IChannel? _channel;
    
    public Worker(
        ILogger<Worker> logger,
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker iniciando...");
        
        await ConnectToRabbitMq(stoppingToken);
        
        if (_channel is null)
        {
            _logger.LogError("Falha ao conectar ao RabbitMQ. Worker encerrando.");
            return;
        }
        
        await DeclareQueueAndBind(_settings.NotificationQueueName, _settings.InventoryReservedRoutingKey, stoppingToken);

        _logger.LogInformation(
            "Queue '{Queue}' vinculada ao Exchange '{Exchange}' com RoutingKey '{RoutingKey}'",
            _settings.NotificationQueueName, _settings.ExchangeName, _settings.InventoryReservedRoutingKey);
        
        // Configurando a QoS para 1 mensagem por vez
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);
        
        // Criando o consumidor
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            await ProcessMessageAsync(ea, stoppingToken);
        };
        _logger.LogInformation("NotificationWorker aguardando mensagens...");
        
        // Realiza o consumo das mensagens
        await _channel.BasicConsumeAsync(
            queue: _settings.NotificationQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
        
        // Manter o Worker rodando até ser cancelado
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task DeclareQueueAndBind(string queueName, string routingKey, CancellationToken stoppingToken)
    {
        // Cria a fila
        await _channel!.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // Linkando a fila ao exchange
        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            cancellationToken: stoppingToken);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            // Deserializando a mensagem
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var orderMessage = JsonSerializer.Deserialize<Order>(json);

            if (orderMessage is null)
            {
                _logger.LogWarning("Mensagem inválida recebida. Descartando...");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                
                return;
            }
            
            // Cria escopo para o bd
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Buscando o pedido pelo id
            var order = await context.Orders.FindAsync(orderMessage.Id);

            if (order is null)
            {
                _logger.LogWarning("Pedido {OrderId} não encontrado no banco de dados. Descartando...", orderMessage.Id);
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                
                return;
            }

            // Simulando envio de e-mail
            _logger.LogInformation("Enviando notificação para email o {CustomerEmail} referente ao pedido {OrderId}", order.CustomerEmail, order.Id);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            
            _logger.LogInformation("Notificação via e-mail ENVIADA para o cliente {CustomerEmail}. Pedido {OrderId} - {status}", order.CustomerEmail, order.Id, order.OrderStatus.ToString());
            
            // Confirmando o processamento da mensagem
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            
            _logger.LogInformation("Notificação do pedido {OrderId} processada com sucesso.", order.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erro ao processar mensagem.");
            
            // NACK - rejeita a mensagem (requeue: true para tentar novamente)
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }

    private async Task ConnectToRabbitMq(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }
        
        // Criando a factory com as configurações
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };
            
        // Criando a conexão e canal
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken:cancellationToken);
            
        _logger.LogInformation("Conectando ao RabbitMQ em {HostName}:{Port}", _settings.HostName, _settings.Port);
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NotificationWorker encerrando...");
        
        _channel?.Dispose();
        _connection?.Dispose();
        
        await base.StopAsync(cancellationToken);
    }
}
