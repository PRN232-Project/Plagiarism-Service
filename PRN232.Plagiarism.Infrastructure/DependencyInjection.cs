using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace PRN232.Plagiarism.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DB Connection
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<Persistence.PlagiarismDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register infrastructure services (e.g. DbContext, Repositories) here
        services.AddScoped<PRN232.Plagiarism.Application.Interfaces.IPlagiarismRepository, PRN232.Plagiarism.Infrastructure.Persistence.Repositories.PlagiarismRepository>();
        services.AddScoped<PRN232.Plagiarism.Application.Interfaces.IPlagiarismAlertPublisher, PRN232.Plagiarism.Infrastructure.Messaging.PlagiarismAlertPublisher>();
        services.AddHostedService<PRN232.Plagiarism.Infrastructure.Messaging.IntegrationEventConsumer>();

        return services;
    }
}
