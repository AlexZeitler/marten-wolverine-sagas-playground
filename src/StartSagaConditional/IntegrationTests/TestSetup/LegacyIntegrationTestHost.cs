using System.Reflection;
using LegacyIntegration;
using Marten;
using Microsoft.Extensions.Hosting;
using Projects;

namespace IntegrationTests.TestSetup;

public static class LegacyIntegrationTestHost
{
  public static async Task<IntegrationTestHost> InitializeAsync(
    Action<StoreOptions>? configureStoreOptions = null
  )
  {
    var testEventStore = await TestEventStore.InitializeAsync("legacy", configureStoreOptions);

    var configuration = new TestConfiguration
      {
        ["ConnectionStrings:EventStore"] = testEventStore.MasterDbConnectionString,
      }
      .AsConfigurationRoot();


    return await IntegrationTestHost.InitializeAsync(
      testEventStore,
      builder => builder.ConfigureLegacyIntegration(configuration),
      configuration
    );
  }
}
