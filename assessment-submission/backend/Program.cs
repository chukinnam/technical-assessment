using System.Text.Json.Serialization;
using CourseInquiryApi.Auth;
using CourseInquiryApi.Data;
using CourseInquiryApi.Services;
using CourseInquiryApi.Services.Crm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---- Swagger / OpenAPI (use it for create/update operations) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Lets the Swagger "Authorize" button send the X-Api-Key header for protected endpoints.
    c.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
    {
        Name = ApiKeyAuthenticationHandler.HeaderName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API key for protected (admin) endpoints. Header: X-Api-Key"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ApiKeyAuthenticationHandler.SchemeName
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---- Database:
var connectionString = builder.Configuration.GetConnectionString("SqlServer");
builder.Services.AddDbContext<AppDbContext>(options =>options.UseSqlServer(connectionString));

// ---- API key authentication
builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, null);

//  register services ----
builder.Services.AddScoped<IInquiryService, InquiryService>();
builder.Services.AddScoped<ICrmConnector, CrmConnector>(); 
builder.Services.AddScoped<CrmSyncDispatcher>();
builder.Services.AddHostedService<CrmDispatchWorker>(); // polls the CrmSyncLogs outbox

// ---- CORS 
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
