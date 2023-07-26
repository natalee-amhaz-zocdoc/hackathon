using System.Linq;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;
using FaqChatbot.Cron;
using Zocdoc.DependencyInjection.AutofacLoader;
using Zocdoc.Extensions.Collections;
using Zocdoc.Http.SpecConfig;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureSentry() //todo: this should be extracted to zd-dotnet and also used in the Worker project (should also work for lambda)
    .ConfigureTracing() //todo: this should be extracted to zd-dotnet and also used in the Worker project (should also work for lambda)
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
            configBuilder.AddSpecConfiguration();
            //configBuilder.AddSystemsManager("/some/key"); //if loading settings from SSM
        })
    .UseServiceProviderFactory(
        new AutofacServiceProviderFactory(
            c =>
                new ZdAutofacLoader().RegisterWithContainerBuilder(c, isFakeService: false)))
    .Build()
    .RunAsync();
