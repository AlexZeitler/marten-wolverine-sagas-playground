using IntegrationTests.TestSetup;
using LegacyIntegration.StartProjectFromIntegration;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Projects.Features.StartNewProject;
using Shouldly;

namespace IntegrationTests;

[TestFixture]
public class When_starting_a_project_from_legacy_integration
{
  private IntegrationTestHost _legacyHost;
  private IntegrationTestHost _projectsHost;

  [SetUp]
  public async Task InitializeAsync()
  {
    _legacyHost = await LegacyIntegrationTestHost.InitializeAsync();
    _projectsHost = await ProjectsIntegrationTestHost.InitializeAsync();

    await _legacyHost.Host.Scenario(
      _ =>
      {
        _.Post.Json(new StartIntegrationProject("integration-project-1", "Integration Project 1"))
          .ToUrl("/integration-projects");
        _.StatusCodeShouldBe(204);
      }
    );

    await Task.Delay(2000);
  }

  [Test]
  public async Task should_create_project()
  {
    var store = _projectsHost.Services.GetRequiredService<IDocumentStore>();
    await using var session = store.LightweightSession();
    var project = await session.Query<Project>()
      .FirstOrDefaultAsync();
    project.ShouldNotBeNull();
    project.ProjectName.ShouldBe("Integration Project 1");
  }

  [TearDown]
  public async Task DisposeAsync()
  {
    await _legacyHost.DisposeAsync();
    await _projectsHost.DisposeAsync();
  }
}
