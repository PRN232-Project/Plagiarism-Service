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
    private readonly string _hostName;

    public PlagiarismAlertPublisher(IConfiguration configuration)
    {
        _hostName = configuration["RabbitMQ:HostName"] 
                    ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST") 
                    ?? "localhost";
    }

    public async Task PublishAlertAsync(PlagiarismAlertEvent alertEvent)
    {
        var factory = new ConnectionFactory { HostName = _hostName };
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
