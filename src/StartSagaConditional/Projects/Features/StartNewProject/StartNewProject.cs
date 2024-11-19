using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Wolverine.Http;
using Wolverine.Persistence.Sagas;

namespace Projects.Features.StartNewProject;

public record StartNewProject(string ProjectId, string ProjectName);

public record NewProjectStarted([property: SagaIdentity] string ProjectId, string ProjectName);

public record Project(string ProjectName)
{
  public string? Id { get; init; }
}

public class ProjectProjection : SingleStreamProjection<Project>
{
  public static Project Create(
    IEvent<NewProjectStarted> @event
  ) => new(@event.Data.ProjectName)
  {
    Id = @event.StreamKey!
  };
}

public class StartNewProjectHandler
{
  [WolverinePost("/projects")]
  public async Task<object[]> HandleAsync(
    StartNewProject command,
    IDocumentStore documentStore,
    ILogger logger
  )
  {
    var (projectId, projectName) = command;
    logger.LogDebug("Starting new project: {ProjectName}", projectName);

    await using var session = documentStore.LightweightSession();
    var started = new NewProjectStarted(projectId, projectName);

    session.Events.StartStream<Project>($"project-{projectId}", started);
    await session.SaveChangesAsync();

    return [started];
  }
}
