using System.Reflection;
using ExcelBotCs.Services;

namespace ExcelBotCs.Extensions;

public static class ServiceExtensions
{
    public static void AddDatabaseServices(this IServiceCollection services)
    {
        // register all database services automatically through reflection
        var serviceTypes = Assembly
            .GetExecutingAssembly()
            .GetTypesFromInterface(typeof(IBaseDatabaseService<>));

        foreach (var serviceType in serviceTypes)
        {
            services.AddSingleton(serviceType);
        }
    }
}