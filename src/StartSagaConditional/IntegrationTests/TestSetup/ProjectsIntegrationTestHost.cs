using System.Reflection;
using Marten;
using Projects;
using Projects.Features.StartNewProject;

namespace IntegrationTests.TestSetup;

public static class ProjectsIntegrationTestHost
{
  public static async Task<IntegrationTestHost> InitializeAsync(
    Action<StoreOptions>? configureStoreOptions = null
  )
  {
    var testEventStore = await TestEventStore.InitializeAsync("projects", configureStoreOptions);

    var configuration = new TestConfiguration
      {
        ["ConnectionStrings:EventStore"] = testEventStore.MasterDbConnectionString,
      }
      .AsConfigurationRoot();

    return await IntegrationTestHost.InitializeAsync(
      testEventStore,
      builder => builder.ConfigureProjects(configuration),
      configuration
    );
  }
}
