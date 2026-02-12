using Microsoft.AspNetCore.Http.Json;
using Serilog;
using SetupIts.Hosting;
using SetupIts.Presentation;
using SetupIts.Presentation.AppCore.JsonConverters;

var builder = WebApplication.CreateBuilder(args);
builder.Host
      .UseSerilog((context, services, configuration) =>
      {
          configuration.ReadFrom.Configuration(context.Configuration);
      })
      .ConfigureAppConfiguration((hostingContext, config) =>
      {
          var currentEnv = ServiceInfoHelper.GetEnvValue(
              hostingContext.Configuration,
              ["env", "", "DOTNET_ENVIRONMENT", "ASPNETCORE_ENVIRONMENT"],
              "Development");

          var serviceName = ServiceInfoHelper.GetEntryProjectName(config.Build());
          Console.Title = $"{serviceName}({currentEnv})";
          Console.WriteLine($"Service = {serviceName}, Env = {currentEnv}");

          config.SetBasePath(Path.Combine(AppContext.BaseDirectory))
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{currentEnv}.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
      })
      .ConfigureServices((hostBuilderContext, services) =>
      {
          builder.Services.AddOpenApi();
          builder.Services.AddEndpointsApiExplorer();
          builder.Services.AddSwaggerGen();
          builder.Services.Configure<JsonOptions>(options =>
          {
              options.SerializerOptions.Converters.Add(new ProductIdJsonConverter());
              options.SerializerOptions.Converters.Add(new QuantityJsonConverter());
              options.SerializerOptions.Converters.Add(new UnitPriceJsonConverter());
          });

          ServiceInstallerHelper.InstallServicesRecursively(
              services,
              hostBuilderContext.Configuration,
              false,
             [PresentationAssemblyReference.Assembly]);
      });

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();
app.MapAllMiddlewares();

app.MapFallbackToFile("/index.html");

app.Run();



