using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessing.Core.Entities;
using RabbitMQ.Client;

namespace OrderProcessing.Infrastructure.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;
    
    public RabbitMqPublisher(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }
    
    public async Task PublishOrderCreatedAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            // Garante que a conexão esteja funcionando
            await EnsureConnectionAsync(cancellationToken);

            if (_channel is null)
            {
                _logger.LogWarning("Canal RabbitMQ não disponível. Envio de mensagem cancelado.");
                return;
            }
            
            // Cria o exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
            
            // Serializa o objeto para JSON
            var message = JsonSerializer.Serialize(order);
            var body = Encoding.UTF8.GetBytes(message);
            
            // Define propriedades da mensagem
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };
            
            // Publica a mensagem
            await _channel.BasicPublishAsync(
                exchange: _settings.ExchangeName,
                routingKey: _settings.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "Mensagem publicada no RabbitMQ. OrderId: {OrderId}, Exchange: {Exchange}, RoutingKey: {RoutingKey}",
                order.Id, 
                _settings.ExchangeName, 
                _settings.RoutingKey);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Falha ao publicar a mensagem no RabbitMQ. OrderId: {OrderId}", order.Id);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _channel?.Dispose();
        _connection?.Dispose();
        
        _disposed = true;
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        try
        {
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
        catch (Exception e)
        {
            _logger.LogError(e, "Falha ao conectar ao RabbitMQ em {HostName}:{Port}", _settings.HostName, _settings.Port);
            
            // Limpando a conexão e canal 
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
        }
    }
}