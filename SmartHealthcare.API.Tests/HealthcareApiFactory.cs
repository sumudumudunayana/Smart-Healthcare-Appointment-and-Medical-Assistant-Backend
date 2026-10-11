using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using SmartHealthcare.API.Data;

namespace SmartHealthcare.API.Tests;

public class HealthcareApiFactory : WebApplicationFactory<Program>
{
    private const string TestJwtKey =
        "integration-test-key-at-least-32-bytes-long";

    private const string TestJwtIssuer =
        "SmartHealthcareIntegrationTests";

    private const string TestJwtAudience =
        "SmartHealthcareIntegrationTests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Generate the database name once per factory instance.
        string databaseName =
            $"SmartHealthcareIntegrationTests-{Guid.NewGuid()}";

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Database=SmartHealthcareTest;Username=test;Password=test",

                    ["Jwt:Key"] = TestJwtKey,
                    ["Jwt:Issuer"] = TestJwtIssuer,
                    ["Jwt:Audience"] = TestJwtAudience
                });
        });

        // Keep expected authentication challenges from obscuring test results.
        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the production database configuration.
            services.RemoveAll<ApplicationDbContext>();

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<ApplicationDbContext>>();

            // All contexts created by this factory use the same database.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });

            // Explicitly configure JWT validation for integration tests.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestJwtKey));

                    options.TokenValidationParameters.ValidateIssuerSigningKey =
                        true;

                    options.TokenValidationParameters.ValidateIssuer =
                        true;

                    options.TokenValidationParameters.ValidIssuer =
                        TestJwtIssuer;

                    options.TokenValidationParameters.ValidateAudience =
                        true;

                    options.TokenValidationParameters.ValidAudience =
                        TestJwtAudience;

                    options.TokenValidationParameters.ValidateLifetime =
                        true;

                    // Retain zero clock skew for deterministic expiry checks.
                    options.TokenValidationParameters.ClockSkew =
                        TimeSpan.Zero;

                });
        });
    }
}
