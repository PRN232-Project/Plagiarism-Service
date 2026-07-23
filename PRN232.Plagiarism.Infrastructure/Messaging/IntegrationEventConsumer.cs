using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MediatR;
using PRN232.Plagiarism.Application.Events;
using PRN232.Plagiarism.Application.UseCases.CheckPlagiarism;

namespace PRN232.Plagiarism.Infrastructure.Messaging;

public class IntegrationEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public IntegrationEventConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<IntegrationEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--> Plagiarism Integration Event Consumer starting...");
        if (!_configuration.GetValue("RabbitMQ:Enabled", true))
        {
            _logger.LogInformation("--> RabbitMQ consumer is disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                    Port = _configuration.GetValue("RabbitMQ:Port", 5672),
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest"
                };
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // Khai báo queue nhận sự kiện từ Grading Engine
                await _channel.QueueDeclareAsync(
                    queue: "submission-graded",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken
                );

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        _logger.LogInformation("--> Received SubmissionGradedEvent: {Message}", message);

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var @event = JsonSerializer.Deserialize<SubmissionGradedEvent>(message, options);

                        if (@event != null)
                        {
                            await ProcessEventAsync(@event);
                        }

                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "--> Error processing consumed message.");
                        // Nack and requeue
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: "submission-graded",
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken
                );

                _logger.LogInformation("--> Plagiarism Consumer connected and listening to 'submission-graded' queue.");
                
                // Block cho đến khi bị cancel
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "--> RabbitMQ connection failed. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessEventAsync(SubmissionGradedEvent @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        _logger.LogInformation("--> Sending CheckPlagiarismCommand for student {StudentId} (Submission: {SubmissionId})",
            @event.StudentId, @event.SubmissionId);

        var command = new CheckPlagiarismCommand(
            @event.SubmissionId,
            @event.ExamId,
            @event.StudentId,
            @event.WorkspacePath,
            @event.BannedKeywords
        );

        await mediator.Send(command);
        _logger.LogInformation("--> CheckPlagiarismCommand completed successfully.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
        }
        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}
