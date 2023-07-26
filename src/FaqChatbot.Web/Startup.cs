using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Autofac;
using JetBrains.Annotations;
using LocalStack.Client.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Serilog;
using ILogger = Serilog.ILogger;

using Zocdoc.Http.Extensions;
using Zocdoc.Monitoring;
using Zocdoc.Web.Base;
using Zocdoc.Web.Mvc;
using Zocdoc.Web.Service;

namespace FaqChatbot.Web
{
    public class Startup
    {
        protected bool IsFake { get; set; }

        private IConfigurationRoot Configuration { get; }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            env.InitLogging(Configuration);

            
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var zdSettings = Configuration.GetZocdocSettings();
            

            services.AddMvcServices(Configuration);
            services.AddSingleton(typeof(ILogger), Log.Logger);
            services.AddGzipCompression();
            services.AddHostedService<DiagnosticMetricsRecorder>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IHttpRequestAccessor, HttpRequestAccessor>();
            services.AddZocdocHttpClients(zdSettings);

            var contextConfig = new DynamoDBContextConfig
            {
                TableNamePrefix = Configuration["DynamoDBTableNamePrefix"]
            };

            services
                .AddLocalStack(Configuration)
                .AddDefaultAWSOptions(Configuration.GetAWSOptions())
                .AddAwsService<IAmazonDynamoDB>()
                .AddSingleton<IDynamoDBContext>(
                    serviceProvider =>
                    {
                        var dynamoDbClient = serviceProvider.GetRequiredService<IAmazonDynamoDB>();
                        return new DynamoDBContext(dynamoDbClient, contextConfig);
                    });
        }


        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterDefaults(IsFake);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            ILoggerFactory loggerFactory,
            IHostApplicationLifetime appLifetime)
        {
            

            app.ConfigureMiddleware(env, loggerFactory, appLifetime, Configuration);

            if (!env.IsDevelopment())
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
        }
    }
}
