using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using PetFlowAPI.Data;
using PetFlowAPI.HealthChecks;
using PetFlowAPI.Mappings;
using Serilog;
using Serilog.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PetFlowAPI.Security;
using PetFlowAPI.Domain;

const string serviceName = "PetFlowAPI";
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", serviceName)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] TraceId={TraceId} CorrelationId={CorrelationId} {Message:lj}{NewLine}{Exception}"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = serviceName,
        Version = "v1",
        Description = "API para gerenciamento de saúde de pets - FIAP Challenge 2026 | Clyvo Vet"
    });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var connectionString = builder.Configuration.GetConnectionString("OracleConnection");
builder.Services.AddDbContext<PetFlowContext>(options =>
{
    options.UseOracle(connectionString);
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRewardPointCalculator, RewardPointCalculator>();
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PetFlowContext>("oracle-database", tags: new[] { "ready", "database" })
    .AddCheck<ExternalServicesHealthCheck>("external-services", tags: new[] { "ready", "external" });

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(PetFlowMetrics.MeterName)
        .AddConsoleExporter());

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "");
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerValue)
        && !string.IsNullOrWhiteSpace(headerValue) ? headerValue.ToString() : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString() ?? ""))
    {
        var stopwatch = Stopwatch.StartNew();
        try { await next(); }
        finally { PetFlowMetrics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("http.status_code", context.Response.StatusCode)); }
    }
});

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PetFlow API v1"));
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.ContentType = "application/json";
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var error = feature?.Error;
    var innerMessage = error?.InnerException?.Message ?? error?.Message ?? "";
    var fullMessage = error?.ToString() ?? innerMessage;
    Log.Error(error, "Unhandled exception. CorrelationId={CorrelationId}", context.Response.Headers["X-Correlation-ID"].ToString());
    var duplicate = fullMessage.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase) || fullMessage.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    var foreignKey = fullMessage.Contains("ORA-02291", StringComparison.OrdinalIgnoreCase) || fullMessage.Contains("ORA-02292", StringComparison.OrdinalIgnoreCase) || fullMessage.Contains("integrity constraint", StringComparison.OrdinalIgnoreCase);
    context.Response.StatusCode = duplicate || foreignKey ? 409 : 500;
    await context.Response.WriteAsJsonAsync(new
    {
        statusCode = context.Response.StatusCode,
        erro = duplicate ? "Registro duplicado." : foreignKey ? "Operação bloqueada por registros relacionados." : "Erro interno no servidor.",
        detalhe = app.Environment.IsDevelopment() ? fullMessage : null,
        correlationId = context.Response.Headers["X-Correlation-ID"].ToString()
    });
}));

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = HealthResponseWriter.WriteAsync });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = HealthResponseWriter.WriteAsync });
app.Run();

public partial class Program { }

public static class PetFlowMetrics
{
    public const string MeterName = "PetFlowAPI.Metrics";
    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("petflow.http.request.duration", "ms", "Duração das requisições HTTP");
}
