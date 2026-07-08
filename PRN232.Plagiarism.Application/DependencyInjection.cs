using Microsoft.Extensions.DependencyInjection;

namespace PRN232.Plagiarism.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<PRN232.Plagiarism.Application.Interfaces.IPlagiarismScanner, PRN232.Plagiarism.Application.Services.RoslynPlagiarismScanner>();

        return services;
    }
}
