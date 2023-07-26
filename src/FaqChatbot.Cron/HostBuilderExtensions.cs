using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Zocdoc.Extensions.Collections;
using Zocdoc.Settings;
using Zocdoc.Tracing.Extensions;

namespace FaqChatbot.Cron;

public static class HostBuilderExtensions
{
    private const string SentryConfigDsn = "Dsn";

    public static IHostBuilder ConfigureSentry(this IHostBuilder builder)
    {
        // There might not be a generic host builder equivalent of use
        // sentry, yet (https://github.com/getsentry/sentry-dotnet/issues/190).
        // So here we recreate some of the logic that `UseSentry` would invoke.
        return builder.ConfigureLogging((context, logging) =>
        {
            var sentrySection = context.Configuration.GetSection("Sentry");

            var envVarDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
            if (!envVarDsn.IsNullOrEmpty())
            {
                sentrySection[SentryConfigDsn] = envVarDsn;
            }

            if (context.HostingEnvironment.IsProduction() && sentrySection.GetValue<string>(SentryConfigDsn) == null)
            {
                throw new Exception(
                    "Sentry is required in the Production environment. Set SENTRY_DSN to configure.");
            }

            logging.Services.Configure<SentryLoggingOptions>(sentrySection);
            logging.AddSentry(o =>
            {
                // If sentry env has not been set in by env vars, or via appsettings
                // default the environment to be the same as the hosting env.
                if (o.Environment.IsNullOrEmpty())
                {
                    o.Environment = context.HostingEnvironment.EnvironmentName;
                }
            });
        });
    }

    public static IHostBuilder ConfigureTracing(this IHostBuilder builder)
    {
        return builder.ConfigureServices((context, collection) =>
        {
            var serilogLoggerProvider = new SerilogLoggerProvider(Log.Logger);
            var zipkinConfig = context.Configuration.GetZipkinSettings();
            var zocdocSettings = context.Configuration.GetZocdocSettings();

            collection.AddOpenTelemetryTracing(providerBuilder =>
            {
                providerBuilder.AddZocdocTracingSettings(
                    zocdocSettings.ServiceName,
                    zipkinConfig.Endpoint,
                    zocdocSettings.ServiceVersion,
                    serilogLoggerProvider.CreateLogger(nameof(HostBuilderExtensions)),
                    zipkinConfig.SamplingRate
                );
            });
        });
    }
}
