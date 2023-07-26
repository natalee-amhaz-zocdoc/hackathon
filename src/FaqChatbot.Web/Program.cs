using System.Linq;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;
using FaqChatbot.Web;
using Zocdoc.Aws.Extensions;
using Zocdoc.Extensions.Collections;
using Zocdoc.Http.SpecConfig;

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
            configBuilder.PrepareSystemsManagerLocalstack();
            configBuilder.AddSpecConfiguration();
            //configBuilder.AddSystemsManager("/some/key"); //if loading settings from SSM
        })
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureWebHostDefaults(
        webBuilder =>
            webBuilder
                .UseStartup<Startup>()
                .UseZocdocDefaults())
    .Build()
    .RunAsync();
