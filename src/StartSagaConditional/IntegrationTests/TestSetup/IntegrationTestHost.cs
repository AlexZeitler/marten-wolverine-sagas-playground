using System.Reflection;
using Alba;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationTests.TestSetup;

public class TestConfiguration : Dictionary<string, string?>
{
  public TestConfiguration AddSection(
    string key,
    IDictionary<string, string?> values
  )
  {
    foreach (var (k, v) in values)
    {
      this[$"{key}:{k}"] = v;
    }

    return this;
  }

  public IConfigurationRoot AsConfigurationRoot()
  {
    return new ConfigurationBuilder()
      .AddInMemoryCollection(this)
      .Build();
  }
}

public sealed class IntegrationTestHost : IDisposable
{
  public IAlbaHost Host { get; init; }
  private TestEventStore EventStore { get; init; }
  public IServiceProvider Services => Host.Services;

  private IntegrationTestHost(
    IAlbaHost host,
    TestEventStore eventStore
  )
  {
    Host = host;
    EventStore = eventStore;
  }

  public static async Task<IntegrationTestHost> InitializeAsync(
    TestEventStore testEventStore,
    Action<IHostBuilder> configureHostBuilder,
    IConfigurationRoot configuration
  )
  {
    using var factory = LoggerFactory.Create(
      builder =>
      {
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddSimpleConsole(
          options =>
          {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
          }
        );
      }
    );
    var logger = factory.CreateLogger("IntegrationTests");


    var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();
    hostBuilder.ConfigureServices(
      s =>
      {
        s.AddSingleton(logger);
        s.AddSingleton<ILogger, NullLogger<IntegrationTestHost>>();
        s.AddLogging(
          o => o
            .SetMinimumLevel(LogLevel.Information)
            .AddDebug()
            .AddConsole()
        );
      }
    );
    configureHostBuilder.Invoke(hostBuilder);
    hostBuilder.ConfigureAppConfiguration(app => app.AddConfiguration(configuration));

    var albaHost = await hostBuilder.StartAlbaAsync();

    return new IntegrationTestHost(albaHost, testEventStore);
  }


  public async Task DisposeAsync()
  {
    await Host.DisposeAsync();
    await EventStore.DisposeAsync();
  }

  public void Dispose()
  {
    DisposeAsync()
      .GetAwaiter()
      .GetResult();
  }
}
