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
using Mindflow_backend.AiFeedback.Application.Services;
using Mindflow_backend.AiFeedback.Infrastructure.Services;
using Mindflow_backend.AiIntegration.Application.Services;
using Mindflow_backend.AiIntegration.Infrastructure.Services;
using Mindflow_backend.Chat.Application.Services;
using Mindflow_backend.Chat.Infrastructure.Services;
using Mindflow_backend.Habits.Application.Internal.CommandServices;
using Mindflow_backend.Habits.Application.Internal.QueryServices;
using Mindflow_backend.Habits.Domain.Repositories;
using Mindflow_backend.Habits.Infrastructure.Persistence.Ef.Repositories;
using Mindflow_backend.WellnessEngine.Application.Services;
using Mindflow_backend.WellnessContent.Domain.Entities;
using Mindflow_backend.Shared.Infrastructure.Caching;
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

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddHttpClient("Gemini", c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<IAiService, GeminiService>();
builder.Services.AddScoped<IAiFeedbackService, AiFeedbackService>();

builder.Services.AddScoped<IHabitRepository, HabitRepository>();
builder.Services.AddScoped<IHabitCompletionLogRepository, HabitCompletionLogRepository>();
builder.Services.AddScoped<IHabitCommandService, HabitCommandService>();
builder.Services.AddScoped<IHabitLogCommandService, HabitLogCommandService>();
builder.Services.AddScoped<IHabitQueryService, HabitQueryService>();
builder.Services.AddScoped<IHabitLogQueryService, HabitLogQueryService>();
builder.Services.AddScoped<IWellnessService, WellnessService>();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Redis not configured. Run `docker-compose up redis` locally or set it in your environment.");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "mindflow:";
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

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

    if (!await context.WellnessExercises.AnyAsync())
    {
        context.WellnessExercises.AddRange(
            new WellnessExercise
            {
                Type = WellnessExercise.TypeBreathing,
                Name = "Respiración 4-7-8",
                Description = "Inhala 4 segundos, sostén 7, exhala 8. Ideal para calmar la ansiedad rápidamente.",
                DurationSeconds = 76,
                InhaleSeconds = 4,
                HoldSeconds = 7,
                ExhaleSeconds = 8,
                Cycles = 4,
                SortOrder = 1
            },
            new WellnessExercise
            {
                Type = WellnessExercise.TypeBreathing,
                Name = "Respiración Cuadrada (Box Breathing)",
                Description = "Inhala, sostén, exhala y sostén de nuevo, todo por 4 segundos. Usada por atletas y militares para mantener la calma bajo presión.",
                DurationSeconds = 64,
                InhaleSeconds = 4,
                HoldSeconds = 4,
                ExhaleSeconds = 4,
                HoldAfterExhaleSeconds = 4,
                Cycles = 4,
                SortOrder = 2
            },
            new WellnessExercise
            {
                Type = WellnessExercise.TypeMeditation,
                Name = "Micro-meditación de 5 minutos",
                Description = "Una pausa guiada para enfocar tu atención y reducir el estrés durante el día.",
                DurationSeconds = 300,
                SortOrder = 1
            },
            new WellnessExercise
            {
                Type = WellnessExercise.TypeMeditation,
                Name = "Meditación para dormir",
                Description = "Una meditación relajante de 10 minutos para prepararte para descansar.",
                DurationSeconds = 600,
                SortOrder = 2
            });

        await context.SaveChangesAsync();
    }
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
