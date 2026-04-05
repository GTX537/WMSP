using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using WMSP.Api.Data;
using WMSP.Api.Filters;
using WMSP.Api.Hubs;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ===== DbContext =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(config.GetConnectionString("Default")));

// ===== Redis =====
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(config.GetConnectionString("Redis") ?? "localhost:6379"));

// ===== JWT Authentication =====
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Secret"] ?? "WMSP_Default_Secret_Key_2026_Must_Be_32Chars!")),
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"] ?? "WMSP",
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"] ?? "WMSP",
            ValidateLifetime = true,
        };

        // SignalR JWT (通过query string传递)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ===== Services =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, JwtCurrentUser>();
builder.Services.AddScoped<ICheckPlanService, CheckPlanService>();
builder.Services.AddScoped<ICheckTaskService, CheckTaskService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ===== FluentValidation =====
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePlanValidator>();

// ===== Controllers =====
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});

// ===== SignalR =====
builder.Services.AddSignalR();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ===== OpenAPI =====
builder.Services.AddOpenApi();

var app = builder.Build();

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CheckProgressHub>("/hubs/check-progress");

app.Run();
