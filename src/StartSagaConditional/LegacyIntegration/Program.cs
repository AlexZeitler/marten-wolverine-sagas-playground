// See https://aka.ms/new-console-template for more information

using LegacyIntegration.StartProjectFromIntegration;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Npgsql;
using Weasel.Core;
using Wolverine;
using Wolverine.Http;
using Wolverine.Transports.Tcp;

var connectionString = new NpgsqlConnectionStringBuilder()
{
  Host = "localhost",
  Port = 5402,
  Database = "legacy",
  Username = "legacy",
  Password = "123456"
}.ToString();

await Host.CreateDefaultBuilder()
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
          options.Connection(connectionString);
          options.Events.StreamIdentity = StreamIdentity.AsString;
          options.AutoCreateSchemaObjects = AutoCreate.All;

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
    }
  )
  .Build()
  .RunAsync();
