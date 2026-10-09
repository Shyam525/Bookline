using System.Text;
using Bookline.Api.Middleware;
using Bookline.Application.Common.Behaviors;
using Bookline.Application.Common.Interfaces;
using Bookline.Application.Common.Models;
using Bookline.Domain.Entities;
using Bookline.Application.Services.Commands;
using Bookline.Infrastructure.Authentication;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Persistence.Interceptors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Text;
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new InstantJsonConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// MediatR & FluentValidation Pipeline
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateServiceCommand>());
builder.Services.AddValidatorsFromAssemblyContaining<Bookline.Application.Bookings.Commands.CreateBookingCommandValidator>();
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
builder.Services.AddScoped<INotificationService, Bookline.Infrastructure.Services.NotificationService>();
builder.Services.AddScoped<Bookline.Application.Notifications.Handlers.NotificationHandlers>();
builder.Services.AddScoped<Bookline.Application.Payments.Handlers.PaymentHandlers>();
builder.Services.AddScoped<Bookline.Application.Analytics.Handlers.AnalyticsHandlers>();

// Marketplace Discovery, Geo, Maps & Financial Providers
builder.Services.AddSingleton<Bookline.Application.Common.Interfaces.ISearchIntentService, Bookline.Infrastructure.Search.SearchIntentService>();
builder.Services.AddSingleton<Bookline.Application.Common.Interfaces.IGeocodingProvider, Bookline.Infrastructure.Geo.GeocodingProvider>();
builder.Services.AddSingleton<Bookline.Application.Common.Interfaces.IMapProvider, Bookline.Infrastructure.Maps.MapProvider>();
builder.Services.AddScoped<Bookline.Application.Common.Interfaces.IProviderSearchService, Bookline.Infrastructure.Search.ProviderSearchService>();
builder.Services.AddScoped<Bookline.Application.Common.Interfaces.IPaymentProvider, Bookline.Infrastructure.Payments.PaymentProvider>();
builder.Services.AddScoped<Bookline.Application.Common.Interfaces.IPayoutProvider, Bookline.Infrastructure.Payments.PayoutProvider>();
builder.Services.AddScoped<Bookline.Application.Common.Interfaces.ITeamAuthorizationService, Bookline.Infrastructure.Services.TeamAuthorizationService>();
builder.Services.AddHostedService<Bookline.Infrastructure.Jobs.OutboxAndReminderWorker>();

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

bool usePostgres = false;
try
{
    var builderNpgsql = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    using var tcp = new System.Net.Sockets.TcpClient();
    var ar = tcp.BeginConnect(builderNpgsql.Host ?? "localhost", builderNpgsql.Port > 0 ? builderNpgsql.Port : 5432, null, null);
    var success = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(400));
    if (success && tcp.Connected)
    {
        usePostgres = true;
    }
}
catch
{
    usePostgres = false;
}

if (!usePostgres)
{
    Log.Information("Local environment: PostgreSQL not detected on localhost:5432. Initializing Bookline with In-Memory engine and Marketplace Seed.");
}

builder.Services.AddDbContext<BooklineDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();
    if (usePostgres)
    {
        options.UseNpgsql(connectionString)
               .AddInterceptors(interceptor);
    }
    else
    {
        options.UseInMemoryDatabase("BooklineDb")
               .AddInterceptors(interceptor);
    }
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
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapFallbackToFile("index.html");

try
{
    // Apply EF Core Migrations & Seed Default Tenant Automatically on Startup
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<BooklineDbContext>();

            if (db.Database.IsRelational())
            {
                var dbCreator = db.Database.GetService<IRelationalDatabaseCreator>();

                try
                {
                    _ = db.Tenants.IgnoreQueryFilters().Any();
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
                {
                    Log.Information("Database tables missing. Creating PostgreSQL schema from DbContext model...");
                    dbCreator.CreateTables();
                }

                db.Database.ExecuteSqlRaw(@"
                    CREATE EXTENSION IF NOT EXISTS btree_gist;
                    ALTER TABLE ""Tenants"" ADD COLUMN IF NOT EXISTS ""TimeZoneId"" text NOT NULL DEFAULT 'UTC';
                    UPDATE ""Tenants"" SET ""TimeZoneId"" = 'Asia/Kolkata' WHERE ""Slug"" = 'acme-salon' AND (""TimeZoneId"" IS NULL OR ""TimeZoneId"" = 'UTC');
                    ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS no_overlap;
                    ALTER TABLE ""Bookings"" ADD CONSTRAINT no_overlap
                      EXCLUDE USING gist (
                        ""StaffId"" WITH =,
                        tstzrange(""StartUtc"", ""EndUtc"") WITH &&
                      )
                      WHERE (""Status"" IN ('Pending','Confirmed'));
                ");
            }
            else
            {
                db.Database.EnsureCreated();
            }

            // Seed demo tenant if empty
            if (!db.Tenants.IgnoreQueryFilters().Any())
            {
                var tenant = new Tenant 
                { 
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                    Name = "Bookline Demo Salon", 
                    Slug = "acme-salon",
                    TimeZoneId = "Asia/Kolkata"
                };

                db.Tenants.Add(tenant);

                var category = new ServiceCategory
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    TenantId = tenant.Id,
                    Name = "Hair Services",
                    Description = "Haircuts and styling services"
                };
                db.ServiceCategories.Add(category);

                var service = new Service 
                { 
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    TenantId = tenant.Id, 
                    CategoryId = category.Id,
                    Name = "Haircut & Style", 
                    DurationMinutes = 45, 
                    BufferMinutes = 15, 
                    Price = 50.00m 
                };
                db.Services.Add(service);

                var staff = new Staff 
                { 
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), 
                    TenantId = tenant.Id, 
                    Name = "Alex Johnson", 
                    Email = "alex.johnson@example.com",
                    TimeZoneId = "America/New_York" 
                };
                db.Staff.Add(staff);

                db.StaffServices.Add(new StaffService
                {
                    TenantId = tenant.Id,
                    StaffId = staff.Id,
                    ServiceId = service.Id
                });

                // Add Working Hours for Monday - Friday 09:00 - 17:00
                for (int day = 1; day <= 5; day++)
                {
                    db.WorkingHours.Add(new WorkingHours
                    {
                        TenantId = tenant.Id,
                        StaffId = staff.Id,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(17, 0)
                    });
                }

                var customer = new Customer
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    TenantId = tenant.Id,
                    FirstName = "Jane",
                    LastName = "Doe",
                    Email = "jane.doe@example.com",
                    Phone = "+15550000000"
                };
                db.Customers.Add(customer);

                db.SaveChanges();
                Log.Information("Demo tenant 'acme-salon' and initial seed data created successfully.");
            }

            var demoTenant = db.Tenants.IgnoreQueryFilters()
                .FirstOrDefault(tenant => tenant.Slug == "acme-salon");
            const string demoOwnerEmail = "demo@bookline.local";
            if (app.Environment.IsDevelopment() && demoTenant != null && !db.Users.IgnoreQueryFilters()
                    .Any(user => user.Email.ToLower() == demoOwnerEmail))
            {
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                db.Users.Add(new AppUser
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    TenantId = demoTenant.Id,
                    Email = demoOwnerEmail,
                    PasswordHash = passwordHasher.HashPassword("BooklineDemo123!"),
                    FirstName = "Demo",
                    LastName = "Owner",
                    Role = "Owner"
                });
                db.SaveChanges();
                Log.Information("Demo owner account seeded for tenant 'acme-salon'.");
            }

            // Seed comprehensive marketplace data (Multi-vendor, products, orders, reviews, demo accounts)
            var marketplaceHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            await Bookline.Infrastructure.Persistence.Seed.MarketplaceDbSeeder.SeedAsync(db, marketplaceHasher, logger);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying EF Core database migrations or seeding.");
        }
    }

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

public class InstantJsonConverter : JsonConverter<Instant>
{
    public override Instant Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (string.IsNullOrEmpty(str)) return default;
        var parseResult = InstantPattern.ExtendedIso.Parse(str);
        return parseResult.Success ? parseResult.Value : default;
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, Instant value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public partial class Program { }
