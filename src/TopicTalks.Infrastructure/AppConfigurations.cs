﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using TopicTalks.Infrastructure.Persistence;
using TopicTalks.Infrastructure.Services.Email;
using TopicTalks.Infrastructure.Services.Cloud;
using TopicTalks.Infrastructure.Services.Token;
using TopicTalks.Domain.Enums;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using TopicTalks.Application.Validators;

namespace TopicTalks.Infrastructure;

public static class AppConfigurations
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHttpContextAccessor()
            .AddMemoryCache()
            .Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)))
            .Configure<GoogleCloudSettings>(configuration.GetSection(nameof(GoogleCloudSettings)))
            .AddAuthorization()
            .AddAuthentication(configuration)
            .AddCorsConfiguration()
            .AddDatabase(configuration)
            .AddFluentValidator()
            .AddEmailSettings(configuration)
            .AddGoogleCloud(configuration)
            .AddDependencies()
            .AddHealthChecks();

        return services;
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.ApplyMigration();
        }

        app.UseCustomCors()
           .UseHttpsRedirection()
           .UseHostFiltering()
           .UseAuthentication()
           .UseAuthorization()
           .UseSerilogRequestLogging();

        return app;
    }

    #region ### Database ###

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
            connectionString,
            m => m.MigrationsHistoryTable("MigrationsHistory"))
        );

        return services;
    }

    private static IApplicationBuilder ApplyMigration(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        if (!databaseCreator.Exists())
        {
            dbContext.Database.Migrate();
        }

        return app;
    }

    #endregion

    #region ### FluentValidator ###

    private static IServiceCollection AddFluentValidator(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters()
                .AddValidatorsFromAssemblyContaining(typeof(Application.Dependencies));


        return services;
    }

    #endregion

    #region ### Email ###

    private static IServiceCollection AddEmailSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new EmailSettings();
        configuration.Bind(nameof(EmailSettings), settings);

        services
            .AddFluentEmail(settings.Email, settings.Name)
            .AddSmtpSender(settings.Server, settings.Port, settings.Email, settings.Password);

        return services;
    }

    #endregion

    #region ### CORS ###

    private static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options => {
            options.AddPolicy("AllowSpecificOrigins",
                policyBuilder => {
                    policyBuilder.WithOrigins("https://*.rawfin.net")
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });

            options.AddPolicy("AllowAnyOrigin",
                policyBuilder => {
                    policyBuilder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        return services;
    }

    private static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var policyName = env.IsDevelopment() ? "AllowAnyOrigin" : "AllowSpecificOrigins";

        app.UseCors(policyName);
        return app;
    }

    #endregion

    #region ### Authentication & Authorization ###

    private static IServiceCollection AddAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options => {
            options.AddPolicy(nameof(RoleType.Student), policy => policy.RequireRole(nameof(RoleType.Student)));
            options.AddPolicy(nameof(RoleType.Teacher), policy => policy.RequireRole(nameof(RoleType.Teacher)));
            options.AddPolicy(nameof(RoleType.Moderator), policy => policy.RequireRole(nameof(RoleType.Moderator)));
        });

        return services;
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.Bind(nameof(JwtSettings), jwtSettings);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = "https://rawfin.net",
                    ValidAudience = "https://rawfin.net",
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }

    #endregion

    #region ### Google Cloud ###

    private static IServiceCollection AddGoogleCloud(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new GoogleCloudSettings();
        configuration.Bind(nameof(GoogleCloudSettings), settings);

        services.AddSingleton(serviceProvider => {
            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            var logger = serviceProvider.GetRequiredService<ILogger>();

            return new GoogleConfigurations(memoryCache, settings, logger).GetDriveService();
        });

        return services;
    }

    #endregion

    #region ### Serilog ###

    public static IHostBuilder UseSerilogSqlServer(this IHostBuilder hostBuilder, IConfiguration configuration, IHostEnvironment env)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        hostBuilder.UseSerilog((context, config) =>
            config.ReadFrom.Configuration(context.Configuration)
                .WriteTo.Console(env.IsDevelopment() ? LogEventLevel.Information : LogEventLevel.Error)
                .WriteTo.MSSqlServer(
                    restrictedToMinimumLevel: LogEventLevel.Warning,
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions {
                        TableName = "LogEvents",
                        AutoCreateSqlTable = false
                    })
            );

        return hostBuilder;
    }

    #endregion
}