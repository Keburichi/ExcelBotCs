using System.Reflection;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Extensions;

public static class ConfigExtensions
{
    public static void LoadSettings(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        // Load config params from all different sources
        config
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true,
                reloadOnChange: true)
            .AddEnvironmentVariables();

        services.LoadSettings(config);
    }

    /// <summary>
    ///     Registers every <see cref="Attributes.OptionsSectionAttribute" /> options type from an already-built
    ///     configuration. Split out from the <see cref="WebApplicationBuilder" /> overload so tests can bind the
    ///     real provider stack (JSON file plus environment variables) without starting a web host.
    /// </summary>
    public static void LoadSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsAttributes = Assembly.GetExecutingAssembly().GetOptionTypes();

        foreach (var optionsAttribute in optionsAttributes)
        {
            RegisterOptionsBySection(services, configuration, optionsAttribute.Type, optionsAttribute.Attribute!.Name);
        }

        // WebBaseUrl is a top-level scalar rather than a section, so the attribute-driven binder cannot reach it.
        // It falls back to Jwt:Issuer (spec §7) and an empty value is reported by the lottery startup check
        // instead of failing the host.
        services.AddOptions<WebBaseUrlOptions>()
            .Configure<IConfiguration>((options, config) => options.BaseUrl = WebBaseUrlOptions.Resolve(config));
    }

    private static void RegisterOptionsBySection(IServiceCollection services, IConfiguration configuration,
        Type optionsType, string sectionName)
    {
        var section = configuration.GetSection(sectionName);

        var addOptionsGeneric = typeof(OptionsServiceCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(OptionsServiceCollectionExtensions.AddOptions)
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length == 1)
            .MakeGenericMethod(optionsType);

        var optionsBuilder = addOptionsGeneric.Invoke(null, new object[] { services })!;

        // Use extension methods via their static classes, since they are not instance methods on OptionsBuilder<T>
        var optionsBuilderType = typeof(OptionsBuilder<>).MakeGenericType(optionsType);

        // Bind: OptionsBuilderConfigurationExtensions.Bind<TOptions>(OptionsBuilder<TOptions>, IConfiguration)
        var bindGeneric = typeof(OptionsBuilderConfigurationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(OptionsBuilderConfigurationExtensions.Bind)
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length == 2)
            .MakeGenericMethod(optionsType);
        bindGeneric.Invoke(null, new[] { optionsBuilder, section });

        // ValidateDataAnnotations: OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations<TOptions>(OptionsBuilder<TOptions>)
        var validateDataAnnotationsGeneric = typeof(OptionsBuilderDataAnnotationsExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations)
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length == 1)
            .MakeGenericMethod(optionsType);
        validateDataAnnotationsGeneric.Invoke(null, new[] { optionsBuilder });

        // ValidateOnStart: OptionsBuilderExtensions.ValidateOnStart<TOptions>(OptionsBuilder<TOptions>)
        var validateOnStartGeneric = typeof(OptionsBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(OptionsBuilderExtensions.ValidateOnStart)
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length == 1)
            .MakeGenericMethod(optionsType);
        validateOnStartGeneric.Invoke(null, new[] { optionsBuilder });
    }
}