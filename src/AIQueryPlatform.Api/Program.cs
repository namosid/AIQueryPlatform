using Serilog;
using AIQueryPlatform.Api.Middleware;
using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Services;
using AIQueryPlatform.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/aiquery-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        // Serialize enums as strings instead of integers
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AI Query Platform API",
        Version = "v1",
        Description = "Multi-tenant AI-powered query and reporting platform"
    });

    // Add API key header to Swagger
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "x-api-key",
        Description = "Tenant API Key"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow any origin including 'null' for local file access
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add memory cache
builder.Services.AddMemoryCache();

// Add response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Register scoped TenantContext
builder.Services.AddScoped<TenantContext>();

// Register services
builder.Services.AddSingleton<ITenantService, TenantService>();
builder.Services.AddScoped<INLToSqlService, NLToSqlService>();
builder.Services.AddScoped<ISqlValidatorService, SqlValidatorService>();
builder.Services.AddScoped<IQueryExecutionService, QueryExecutionService>();
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<IIntelligenceLayerService, IntelligenceLayerService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IQueryOrchestrationService, QueryOrchestrationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

// Enable response compression for better performance
app.UseResponseCompression();

// IMPORTANT: CORS must be called before custom middleware and authorization
app.UseCors();

app.UseHttpsRedirection();

// Add custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

Log.Information("AI Query Platform API starting...");

// Pre-warm schema cache on startup for faster first queries
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
        
        var tenants = await tenantService.GetAllTenantsAsync();
        var activeTenants = tenants.Where(t => t.IsActive).ToList();
        
        Log.Information("Pre-warming schema cache for {Count} active tenant(s)", activeTenants.Count);
        
        foreach (var tenant in activeTenants)
        {
            try
            {
                await schemaService.GetDatabaseSchemaAsync(tenant.ConnectionString, tenant.TenantId);
                Log.Information("Pre-loaded schema for tenant {TenantName} ({TenantId})", tenant.Name, tenant.TenantId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to pre-load schema for tenant {TenantName} ({TenantId})", tenant.Name, tenant.TenantId);
            }
        }
        
        Log.Information("Schema cache pre-warming completed");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during schema cache pre-warming");
    }
});

app.Run();
