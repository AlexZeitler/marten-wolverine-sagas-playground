using System.Reflection;
using LegacyIntegration.StartProjectFromIntegration;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Weasel.Core;
using Wolverine;
using Wolverine.Http;
using Wolverine.Transports.Tcp;

namespace LegacyIntegration;

public static class ConfigureHostBuilder
{
  public static IHostBuilder ConfigureLegacyIntegration(
    this IHostBuilder hostBuilder,
    IConfigurationRoot configuration
  )
  {
    hostBuilder
      .ConfigureAppConfiguration(
        (
          _,
          config
        ) => config.AddConfiguration(configuration)
      )
      .ConfigureWebHostDefaults(
        builder =>
        {
          builder
            .UseKestrel(
              options => { options.ListenAnyIP(5001); }
            )
            .Configure(
              app => app
                .UseRouting()
                .UseEndpoints(
                  endpoints => endpoints.MapWolverineEndpoints()
                )
            );
        }
      )
      .ConfigureServices(
        services => services
          .AddWolverineHttp()
          .AddMarten(
            options =>
            {
              options.Connection(
                configuration.GetConnectionString("EventStore") ?? throw new
                  InvalidOperationException("EventStore connection string is missing.")
              );
              options.Events.StreamIdentity = StreamIdentity.AsString;
              options.AutoCreateSchemaObjects = AutoCreate.All;

              options.DisableNpgsqlLogging = true;

              options.Projections.Add<IntegrationProjectProjection>(ProjectionLifecycle.Inline);
            }
          )
      )
      .UseWolverine(
        options =>
        {
          options.ListenAtPort(5003);

          options.PublishAllMessages()
            .ToPort(5002);

          options.PublishAllMessages()
            .ToPort(5003);

          options.Discovery.IncludeAssembly(Assembly.GetAssembly(typeof(Program))!);
        }
      );
    return hostBuilder;
  }
}
