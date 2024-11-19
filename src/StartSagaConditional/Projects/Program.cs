// See https://aka.ms/new-console-template for more information

using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Npgsql;
using Projects.Features.StartNewProject;
using Wolverine;
using Wolverine.Http;
using Wolverine.Transports.Tcp;

var hostBuilder = Host.CreateDefaultBuilder(args);

var martenConnectionString = new NpgsqlConnectionStringBuilder()
{
  Host = "localhost",
  Port = 5401,
  Database = "projects",
  Username = "projects",
  Password = "123456"
}.ToString();

await hostBuilder
  .ConfigureWebHostDefaults(
    builder =>
    {
      builder
        .UseKestrel(options => options.ListenAnyIP(5000))
        .Configure(
          app =>
          {
            app.UseRouting()
              .UseEndpoints(endpoints => endpoints.MapWolverineEndpoints());
          }
        );
    }
  )
  .ConfigureServices(
    services =>
    {
      services
        .AddWolverineHttp()
        .AddMarten(
          options =>
          {
            options.Connection(martenConnectionString);
            options.Events.StreamIdentity = StreamIdentity.AsString;
            options.Projections.Add<ProjectProjection>(ProjectionLifecycle.Inline);
          }
        );
    }
  )
  .UseWolverine(
    options =>
    {
      options
        .ListenAtPort(5002);

      options
        .PublishAllMessages()
        .ToPort(5003);

      options
        .PublishAllMessages()
        .ToPort(5002);
    }
  )
  .Build()
  .RunAsync();
