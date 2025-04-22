using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Repositories;
using Infrastructure.Common;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureOptions<IntegrationOptionsSetup>();
        services.AddScoped<IPreskripciaService, PreskripciaService>();
        services.AddScoped<ILiekRepository, LiekRepository>(); // Zaregistruje implementáciu pre ILiekRepository

        // Registrácia ApplicationDbContext iba raz s nastavením MigrationsAssembly:
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly("WebApi") // migrácie budú hľadané v assembly WebApi
            )
        );

        // Registrácia rozhrania IApplicationDbContext, ktoré využíva ApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitialiser>();

        return services;
    }

    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
