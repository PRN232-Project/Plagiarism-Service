using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using PRN232.Plagiarism.Application.Events;
using PRN232.Plagiarism.Application.Interfaces;

namespace PRN232.Plagiarism.Infrastructure.Messaging;

public class PlagiarismAlertPublisher : IPlagiarismAlertPublisher
{
    private readonly IConfiguration _configuration;

    public PlagiarismAlertPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublishAlertAsync(PlagiarismAlertEvent alertEvent)
    {
        if (!_configuration.GetValue("RabbitMQ:Enabled", true)) return;
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            Port = _configuration.GetValue("RabbitMQ:Port", 5672),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Khai báo queue gửi tin
        await channel.QueueDeclareAsync(
            queue: "plagiarism-alerts",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var json = JsonSerializer.Serialize(alertEvent);
        var body = Encoding.UTF8.GetBytes(json);

        // Publish message
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "plagiarism-alerts",
            body: body
        );
    }
}
