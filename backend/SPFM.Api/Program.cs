using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Serilog;
using SPFM.Api.Configuration;
using SPFM.Api.Middleware;
using SPFM.Api.Security;
using SPFM.Application.Abstractions;
using SPFM.Domain.Calculations;
using SPFM.Infrastructure.Persistence;
using SPFM.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Refuse to start on missing configuration rather than failing per-request.
builder.Configuration.ValidateRequiredConfiguration();

// ---- Logging -------------------------------------------------------------
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---- Authentication ------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// ---- Authorization: DEFAULT DENY ----------------------------------------
// Every endpoint requires an authenticated user unless it opts out with
// [AllowAnonymous]. The alternative — remembering [Authorize] on each controller —
// fails open: one forgotten attribute publishes financial data (requirements s21).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ---- CORS ----------------------------------------------------------------
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));
var allowedOrigins = builder.Configuration
    .GetSection($"{CorsSettings.SectionName}:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy(CorsSettings.PolicyName, policy => policy
    .WithOrigins(allowedOrigins)          // never AllowAnyOrigin
    .WithHeaders("Authorization", "Content-Type", "If-Match")
    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
    .WithExposedHeaders("ETag")));        // the frontend needs ETag for If-Match

// ---- Persistence ---------------------------------------------------------
builder.Services.AddDbContext<SpfmDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Application services -----------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IUserDirectory, UserDirectory>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddSingleton<ISpfmCalculator, SpfmCalculator>();  // pure, so safe as a singleton

// ---- Errors --------------------------------------------------------------
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

// ---- Health --------------------------------------------------------------
var healthChecks = builder.Services.AddHealthChecks();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
    healthChecks.AddNpgSql(connectionString, name: "database", tags: ["ready"]);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as strings ("quote", not 0). Numeric enums are unreadable in
        // logs and silently change meaning if a value is ever inserted into the enum.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SPFM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token.",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

// ---- Pipeline ------------------------------------------------------------
// Order matters and is deliberate.
app.UseExceptionHandler();        // must be first, to catch everything after it
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Only in production: in development the HTTP profile has no HTTPS port, and a
    // redirect would break the browser's cross-origin call.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// CORS BEFORE anything that can short-circuit the request. If a redirect or a 401 is
// emitted first, it carries no CORS headers and the browser reports an opaque
// failure instead of the real status.
app.UseCors(CorsSettings.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).AllowAnonymous();

app.Run();

/// <summary>Exposed so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
