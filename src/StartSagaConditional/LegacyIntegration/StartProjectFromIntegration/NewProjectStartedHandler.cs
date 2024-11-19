using Marten;

namespace LegacyIntegration.StartProjectFromIntegration;

public class NewProjectStartedHandler
{
  public async Task<object[]> Handle(
    Projects.Features.StartNewProject.NewProjectStarted projectStarted,
    IDocumentStore store
  )
  {
    var (projectId, projectName) = projectStarted;

    await using var session = store.QuerySession();
    var project = await session.LoadAsync<IntegrationProject>(projectId);
    if (project is not null)
      return [new NewProjectStarted(projectId, projectName)];
    else
      return Array.Empty<object>();
  }
}
