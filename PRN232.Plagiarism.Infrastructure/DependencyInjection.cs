using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace PRN232.Plagiarism.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register infrastructure services (e.g. DbContext, Repositories) here
        return services;
    }
}
