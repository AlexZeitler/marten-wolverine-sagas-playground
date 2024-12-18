using Marten;
using Npgsql;

namespace IntegrationTests.TestSetup;

public class PostgresAdministration
{
  private readonly string _connectionString;

  public PostgresAdministration(
    string connectionString
  )
  {
    _connectionString = connectionString;
  }

  public async Task CreateDatabaseAsync(
    string? databaseName
  )
  {
    await using var connection = new NpgsqlConnection();
    connection.ConnectionString = _connectionString;
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand(
      $"CREATE DATABASE {databaseName}",
      connection
    );
    await command.ExecuteNonQueryAsync();
    await connection.CloseAsync();
  }

  public async Task DropDatabase(
    string? databaseName
  )
  {
    await using var connection = new NpgsqlConnection();
    connection.ConnectionString = _connectionString;
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand(
      $"DROP DATABASE IF EXISTS {databaseName} WITH (FORCE);",
      connection
    );
    await command.ExecuteNonQueryAsync();
    await connection.CloseAsync();
  }

  public async Task<bool> EnsureDatabaseExists(
    string databaseName
  )
  {
    await using var connection = new NpgsqlConnection();
    connection.ConnectionString = _connectionString;

    await connection.OpenAsync();
    await using var command = new NpgsqlCommand(
      $"SELECT 1 FROM pg_database WHERE datname LIKE '{databaseName}'",
      connection
    );

    var result = await command.ExecuteScalarAsync();
    await connection.CloseAsync();

    return result != null;
  }
}

public class TestDatabase
{
  private readonly PostgresAdministration _postgresAdministration;
  private string? _testEventStoreName;
  public string MasterDbConnectionString { get; }

  public TestDatabase(
    string masterDbConnectionString
  )
  {
    MasterDbConnectionString = masterDbConnectionString;
    _postgresAdministration = new PostgresAdministration(
      masterDbConnectionString
    );
  }

  public async Task<string> InitializeAsync(
    string masterDbConnection,
    string dbPrefix
  )
  {
    Task
      .Delay(new Random().Next(50, 100))
      .Wait();

    var dbId = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_fff");
    _testEventStoreName = $"{dbPrefix}_test_{dbId}_{Guid.NewGuid().ToString()[..4]}";
    await _postgresAdministration.CreateDatabaseAsync(
      _testEventStoreName
    );

    var connectionString = new NpgsqlConnectionStringBuilder
    {
      Pooling = false,
      Port = 5435,
      Host = "localhost",
      CommandTimeout = 20,
      Database = _testEventStoreName,
      Password = "123456",
      Username = "postgres"
    }.ToString();

    return connectionString;
  }

  public async Task DropAsync() => await _postgresAdministration.DropDatabase(_testEventStoreName);
}

public class TestEventStore
{
  public string MasterDbConnectionString { get; }
  private TestDatabase TestDatabase { get; }

  private TestEventStore(
    TestDatabase testDatabase,
    string masterDbConnectionString
  )
  {
    TestDatabase = testDatabase;
    MasterDbConnectionString = masterDbConnectionString;
  }

  public static async Task<TestEventStore> InitializeAsync(
    string dbPrefix,
    Action<StoreOptions>? configureStoreOptions = null
  )
  {
    var masterDbConnection = new NpgsqlConnectionStringBuilder
    {
      Pooling = false,
      Port = 5435,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "postgres",
      Password = "123456",
      Username = "postgres"
    }.ToString();
    var testDatabase = new TestDatabase(masterDbConnection);

    var connectionString = await testDatabase.InitializeAsync(masterDbConnection, dbPrefix);


    // var store = DocumentStore.For(
    //   options =>
    //   {
    //     options.Connection(connectionString);
    //     StoreConfiguration.Configure(options);
    //     configureStoreOptions?.Invoke(options);
    //   }
    // );

    return new TestEventStore(
      testDatabase,
      connectionString
    );
  }

  public async Task DisposeAsync()
  {
    await TestDatabase.DropAsync();
    // Store.Dispose();
  }
}
