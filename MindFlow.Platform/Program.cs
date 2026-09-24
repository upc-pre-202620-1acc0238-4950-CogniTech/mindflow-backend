using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Cortex.Mediator.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mindflow_backend.Analytics.Application.Services;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Services;
using Mindflow_backend.Journal.Infrastructure.BackgroundServices;
using Mindflow_backend.Journal.Infrastructure.Services;
using Mindflow_backend.iam.application.Internal.commandservices;
using Mindflow_backend.iam.application.services;
using Mindflow_backend.iam.domain.repositories;
using Mindflow_backend.iam.infrastructure.persistence.entityframeworkcore.repositories;
using Mindflow_backend.iam.infrastructure.services;
using Mindflow_backend.Shared.Domain.Repositories;
using Mindflow_backend.Shared.Infrastructure.Interfaces.AspNetCore.Configuration;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Mindflow_backend.Shared.Interfaces.Rest.ProblemDetails;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Encryption;
using Mindflow_backend.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using System.Security.Claims;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console());

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers(options =>
    options.Conventions.Add(new KebabCaseRouteNamingConvention()))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendPolicy", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"];
        var origins = string.IsNullOrWhiteSpace(frontendUrl)
            ? new[] { "http://localhost:5173" }
            : frontendUrl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Connection string not set.");

    options.UseMySQL(connectionString)
        .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
        .EnableDetailedErrors();

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

var jwtSecret = builder.Configuration["TokenSettings:Secret"]
                ?? throw new InvalidOperationException("JWT Secret not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MindFlow.API",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MindFlow.Frontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddLocalization();

builder.Services.AddRateLimiter(options =>
{
    // Global limit partitioned per authenticated user (or client IP for anonymous requests)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue("user_id")
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    // Stricter limit for credential-guessing surfaces (sign-in, password reset, PIN verify)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<ProblemDetailsFactory>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();

builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
builder.Services.AddScoped<IJournalSearchIndexer, JournalSearchIndexer>();
builder.Services.AddScoped<IAnalyticsCacheInvalidator, AnalyticsCacheInvalidatorStub>();
builder.Services.AddHostedService<JournalSearchBackfillService>();

builder.Services.AddHttpContextAccessor();

var encryptionKey = builder.Configuration["Encryption:AesKey"];
if (string.IsNullOrEmpty(encryptionKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "Encryption:AesKey not configured. Generate one with AesEncryptionService.GenerateKey().");
    // Dev-only fallback; keeps existing local data readable but must never reach production
    encryptionKey = Convert.ToBase64String(new byte[32]);
}
builder.Services.AddSingleton(new AesEncryptionService(encryptionKey));

var searchIndexKey = builder.Configuration["Encryption:SearchIndexKey"];
if (string.IsNullOrEmpty(searchIndexKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "Encryption:SearchIndexKey not configured. Generate one with SearchTokenHasher.GenerateKey().");
    // Dev-only fallback; journal search tokens won't match across restarts but nothing breaks
    searchIndexKey = SearchTokenHasher.GenerateKey();
}
builder.Services.AddSingleton<ISearchTokenHasher>(new SearchTokenHasher(searchIndexKey));

builder.Services.AddCortexMediator([typeof(Program)]);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontendPolicy");
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
