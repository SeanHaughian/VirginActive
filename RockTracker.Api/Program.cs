using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using RockTracker.Api.Clients.TypiCode;
using RockTracker.Api.Middleware;
using RockTracker.Api.Services;
using RockTracker.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

StartupConfigurationValidator.ValidateRequired(builder.Configuration);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "RockTracker.Api")
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Information()
    .WriteTo.Console(new JsonFormatter(renderMessage: true))
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
});

builder.Services.AddSingleton<ApiKeyValidator>();
builder.Services.AddOptions<MemberProfileResilienceOptions>()
    .Bind(builder.Configuration.GetSection(MemberProfileResilienceOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiKeySwaggerGen();
builder.Services.AddSingleton<IRockStore>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    var logger = serviceProvider.GetRequiredService<ILogger<RockStore>>();
    var filePath = configuration["Storage:FilePath"];
    if (string.IsNullOrWhiteSpace(filePath))
    {
        filePath = System.IO.Path.Combine(environment.ContentRootPath, "rocks-store.json");
    }

    return new RockStore(filePath, logger);
});

builder.Services.AddScoped<IRockRequestValidator, RockRequestValidator>();
builder.Services.AddScoped<IRocksService, RocksService>();

builder.Services.AddHttpClient<ITypiCodeClient, TypiCodeClient>(client =>
{
    var baseUrl = builder.Configuration["ExternalApis:TypiCode:BaseUrl"];
    ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl, "ExternalApis:TypiCode:BaseUrl");
    client.BaseAddress = new Uri(baseUrl);
}).AddMemberProfileResilience();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.EnableValidator();
    });
}

app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
