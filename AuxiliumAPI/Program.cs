using AuxiliumAPI.Common.Services;
﻿using AuxiliumAPI.Common.EF;
using AuxiliumAPI.Common.Services;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Filters;
using AuxiliumAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);



var configPath = Environment.GetEnvironmentVariable("AUXILIUM_CONFIG_PATH")
    ?? builder.Configuration["ConfigPath"]
    ?? "\\\\files.wraitheon.net\\Projects\\Auxilium\\aux3-dev.yaml";

builder.Configuration.AddYamlFile(
    configPath,
    optional: false,
    reloadOnChange: true
);



builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();



builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v3", new OpenApiInfo
    {
        Title = "Auxilium API",
        Version = "V3"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<FileUploadOperationFilter>();
});



builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration!["JWT:ValidIssuer"]!,
            ValidAudience = builder.Configuration!["JWT:ValidAudience"]!,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration!["JWT:SecretKey"]!))
        };
    });
builder.Services.AddAuthorization();



builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var originsSection = builder.Configuration.GetSection("API:AllowedOrigins");
        var origins = originsSection.Get<string[]>() ?? Array.Empty<string>();

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});



builder.Services.AddScoped<IMariaDbService, MariaDbService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<ICaseDocumentService, CaseDocumentService>();
builder.Services.AddScoped<IUserDocumentService, UserDocumentService>();
builder.Services.AddScoped<IFileDocumentService, FileDocumentService>();
builder.Services.AddScoped<IMessageDocumentService, MessageDocumentService>();

builder.Services.AddHttpClient<ICaptchaService, CaptchaService>();



builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();



var mariaDbHost = builder.Configuration["Databases:MariaDB:Host"]           ?? throw new InvalidOperationException("MariaDB Host not found");
var mariaDbPort = builder.Configuration["Databases:MariaDB:Port"]           ?? throw new InvalidOperationException("MariaDB Port not found");
var mariaDbUsername = builder.Configuration["Databases:MariaDB:Username"]   ?? throw new InvalidOperationException("MariaDB Username not found");
var mariaDbPassword = builder.Configuration["Databases:MariaDB:Password"]   ?? throw new InvalidOperationException("MariaDB Password not found");
var mariaDbDatabase = builder.Configuration["Databases:MariaDB:Database"]   ?? throw new InvalidOperationException("MariaDB Database not found");

var connectionString = $"Server={mariaDbHost};Port={mariaDbPort};Database={mariaDbDatabase};User={mariaDbUsername};Password={mariaDbPassword};CharSet=utf8mb4;";


builder.Services.AddDbContext<AuxiliumDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        }
    );

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(swaggerUI =>
    {
        swaggerUI.SwaggerEndpoint("/swagger/v3/swagger.json", "Auxilium API V3");
    });
}

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
