using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Projects.Features.StartNewProject;
using Wolverine;
using Wolverine.Http;
using Wolverine.Persistence.Sagas;

namespace LegacyIntegration.StartProjectFromIntegration;

public record NewProjectStarted([property: SagaIdentity] string ProjectId, string ProjectName);

public class IntegrationProjectSaga : Saga
{
  public string? Id { get; set; }

  public object[] Start(
    IntegrationProjectStarted started
  )
  {
    var (projectId, projectName) = started;
    Id = projectId;
    return [new StartNewProject(projectId, projectName)];
  }

  public object[] Handle(
    NewProjectStarted newProjectStarted,
    ILogger logger
  )
  {
    logger.LogInformation("Saga - New Project started: {ProjectName}", newProjectStarted.ProjectName);
    return [];
  }
}

public record StartIntegrationProject(string ProjectId, string ProjectName);

public record IntegrationProjectStarted(string ProjectId, string ProjectName);

public record IntegrationProject(string ProjectId, string ProjectName)
{
  public string? Id { get; set; }
};

public class IntegrationProjectProjection : SingleStreamProjection<IntegrationProject>
{
  public static IntegrationProject Create(
    IEvent<IntegrationProjectStarted> @event
  ) =>
    new(@event.StreamKey!, @event.Data.ProjectName);
}

public class StartIntegrationProjectHandler
{
  [WolverinePost("/integration-projects")]
  public async Task<object[]> Handle(
    StartIntegrationProject command,
    IDocumentStore store
  )
  {
    await using var session = store.LightweightSession();
    var (projectId, projectName) = command;
    var integrationProjectStarted = new IntegrationProjectStarted(projectId, projectName);

    session.Events.StartStream(projectId, integrationProjectStarted);

    await session.SaveChangesAsync();
    return [integrationProjectStarted];
  }
}
