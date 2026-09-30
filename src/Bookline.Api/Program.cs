using System.Text;
using Bookline.Api.Middleware;
using Bookline.Application.Common.Behaviors;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Application.Services.Commands;
using Bookline.Infrastructure.Authentication;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// MediatR & FluentValidation Pipeline
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateServiceCommand>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateServiceCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Multi-Tenancy & Infrastructure DI
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<TenantSaveChangesInterceptor>();
builder.Services.AddTransient<IPasswordHasher, PasswordHasher>();
builder.Services.AddTransient<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<Bookline.Application.Common.Interfaces.ISlotEngine, Bookline.Application.Common.Services.SlotEngine>();
builder.Services.AddSingleton<Bookline.Application.Common.Interfaces.ISlotHoldService, Bookline.Infrastructure.Services.SlotHoldService>();

// Phase 5 Services & DataProtection
builder.Services.AddDataProtection();
builder.Services.AddTransient<IBookingActionTokenService, Bookline.Infrastructure.Services.BookingActionTokenService>();
builder.Services.AddTransient<ICalendarService, Bookline.Infrastructure.Services.CalendarService>();
builder.Services.AddTransient<IEmailSender, Bookline.Infrastructure.Services.MailKitEmailSender>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// Register DbContext & IApplicationDbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Host=localhost;Database=bookline_db;Username=postgres;Password=postgrespassword";

builder.Services.AddDbContext<BooklineDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();
    options.UseNpgsql(connectionString)
           .AddInterceptors(interceptor);
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<BooklineDbContext>());

// Configure JWT Authentication
var secretKey = builder.Configuration["JwtSettings:Secret"] ?? "SuperSecretKeyForBooklineApiThatIsAtLeast32BytesLong!";
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "Bookline";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "BooklineApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configure RBAC Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManageBookings", policy => policy.RequireRole("Owner", "Staff", "Receptionist"));
    options.AddPolicy("ManageServices", policy => policy.RequireRole("Owner"));
    options.AddPolicy("ManageStaff", policy => policy.RequireRole("Owner"));
});

var app = builder.Build();

// HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting Bookline Web API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bookline Web API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
