using System.Linq;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FaqChatbot.Worker;
using Serilog;
using Serilog.Extensions.Logging;
using Zocdoc.DependencyInjection.AutofacLoader;
using Zocdoc.Extensions.Collections;
using Zocdoc.Http.Extensions;
using Zocdoc.Http.SpecConfig;
using Zocdoc.Monitoring;
using Zocdoc.Settings;
using Zocdoc.Tracing.Extensions;

//todo: configure logging + sentry like we do in lambda
await Host
    .CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(
        configBuilder =>
        {
            configBuilder.Sources.OfType<JsonConfigurationSource>().ForEach(
                x =>
                {
                    x.Optional = false; // appsettings.json & appsettings.{env}.json are super-required
                    x.ReloadOnChange = false; // this causes problems on some Macs. Use `watch run` instead.
                });

            // appsettings.json; appsettings.{env}.json; and env vars are already added
            //configBuilder.PrepareSystemsManagerLocalstack();
            //configBuilder.AddSystemsManager("/some/key"); //if loading settings from SSM
            configBuilder.AddSpecConfiguration();
        })
    .UseServiceProviderFactory(
        new AutofacServiceProviderFactory(
            containerBuilder =>
                new ZdAutofacLoader().RegisterWithContainerBuilder(containerBuilder, isFakeService: false)))
    .ConfigureServices(
        (ctx, services) =>
        {
            services
                .AddHostedService<TickerWorker>()
                .AddHostedService<DiagnosticMetricsRecorder>()
                .AddOpenTelemetryTracing(
                    providerBuilder =>
                    {
                        var serilogLoggerProvider = new SerilogLoggerProvider(Log.Logger);
                        var zipkinConfig = ctx.Configuration.GetZipkinSettings();
                        var zocdocSettings = ctx.Configuration.GetZocdocSettings();

                        providerBuilder.AddZocdocTracingSettings(
                            zocdocSettings.ServiceName,
                            zipkinConfig.Endpoint,
                            zocdocSettings.ServiceVersion,
                            serilogLoggerProvider.CreateLogger(nameof(Program)),
                            zipkinConfig.SamplingRate
                        );
                    })
                .AddZocdocHttpClients(ctx.Configuration.GetZocdocSettings());
        })
    .Build()
    .RunAsync();
