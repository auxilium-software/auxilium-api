using AuxiliumAPI.Common.Services;
using AuxiliumAPI.Common.Services.Interfaces;
using AuxiliumAPI.Common.Utilities;
using AuxiliumAPI.Filters;
using AuxiliumAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

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

        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();



builder.Services.AddSwaggerGen(swaggerGen =>
{
    swaggerGen.SwaggerDoc("v3", new OpenApiInfo
    {
        Title = "Auxilium API",
        Version = "V3"
    });

    swaggerGen.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    swaggerGen.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    swaggerGen.OperationFilter<FileUploadOperationFilter>();
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
        var originsSection = builder.Configuration.GetSection("API:CORS:AllowedOrigins");
        var origins = originsSection.Get<string[]>() ?? Array.Empty<string>();

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});



builder.Services.AddScoped<IMariaDbService, MariaDbService>();
builder.Services.AddScoped<ICouchDbService, CouchDbService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<ICaseDocumentService, CaseDocumentService>();
builder.Services.AddScoped<IUserDocumentService, UserDocumentService>();
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddHttpClient<ICaptchaService, CaptchaService>();



builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();



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
