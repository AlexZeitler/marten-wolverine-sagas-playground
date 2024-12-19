﻿// See https://aka.ms/new-console-template for more information

using Projects;

var configuration = new ConfigurationBuilder().AddJsonFile(
    "appsettings.json",
    optional: false,
    reloadOnChange: true
  )
  .Build();


var hostBuilder = Host.CreateDefaultBuilder(args);

await hostBuilder.ConfigureProjects(configuration!)
  .Build()
  .RunAsync();
