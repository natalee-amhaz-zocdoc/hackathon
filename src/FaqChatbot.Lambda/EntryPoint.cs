using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleNotificationService;
using JetBrains.Annotations;
using LocalStack.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zocdoc.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FaqChatbot.Lambda
{
    [UsedImplicitly]
    public class EntryPoint : LambdaBase
    {
        /// <summary>
        /// This overridden method allows us to hook into the host builder and configure additional services
        /// specific to our lambda implementation.
        /// </summary>
        protected override void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services
                .AddAwsService<IAmazonSimpleNotificationService>()
                .Configure<LambdaSettings>(options =>
                    config.GetSection(LambdaSettings.SettingsSection).Bind(options));
        }

        /// <summary>
        /// This method is called for every Lambda invocation.
        /// </summary>
        [UsedImplicitly]
        public async Task DdbEventStreamHandler(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            await ExecuteEventLambda<IHandler>(context, async handler =>
                { await handler.FunctionHandler(dynamoDbEvent, context); });
        }
    }
}
