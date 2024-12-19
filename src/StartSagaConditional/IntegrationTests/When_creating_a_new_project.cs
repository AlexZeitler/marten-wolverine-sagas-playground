using IntegrationTests.TestSetup;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Projects;
using Projects.Features.StartNewProject;
using Shouldly;
using Wolverine;

namespace IntegrationTests;

[TestFixture]
public class When_creating_a_new_project
{
  private IntegrationTestHost _projectsHost;
  private IntegrationTestHost _legacyHost;

  [SetUp]
  public async Task InitializeAsync()
  {
   
    _projectsHost = await ProjectsIntegrationTestHost.InitializeAsync();
    _legacyHost = await LegacyIntegrationTestHost.InitializeAsync();
    var scope = _projectsHost.Services.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
    var startNewProject = new StartNewProject("1", "Some project name");
    await bus.InvokeAsync(startNewProject);

    await Task.Delay(2000);
  }

  [Test]
  public async Task should_create_projection()
  {
    var store = _projectsHost.Services.GetRequiredService<IDocumentStore>();
    await using var session = store.LightweightSession();
    var project = await session.LoadAsync<Project>("project-1");
    project.ShouldNotBeNull();
    project.ProjectName.ShouldBe("Some project name");
  }

  [TearDown]
  public async Task DisposeAsync()
  {
    await _legacyHost.DisposeAsync();
    await _projectsHost.DisposeAsync();
  }
}
