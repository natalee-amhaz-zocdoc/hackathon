using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zocdoc.DependencyInjection;
using Zocdoc.Extensions.Collections;

namespace FaqChatbot.Lambda
{
    public interface IHandler
    {
        Task<StreamsEventResponse> FunctionHandler(DynamoDBEvent dynamoDbEvent, ILambdaContext context);
    }

    [RegisterService]
    public class Handler : IHandler
    {
        private readonly IOptions<LambdaSettings> _lambdaSettings;
        private readonly ILogger<Handler> _logger;
        private readonly IAmazonSimpleNotificationService _snsService;

        public Handler(
            IOptions<LambdaSettings> lambdaSettings,
            ILogger<Handler> logger,
            IAmazonSimpleNotificationService snsService
        )
        {
            _lambdaSettings = lambdaSettings;
            _logger = logger;
            _snsService = snsService;
        }

        public async Task<StreamsEventResponse> FunctionHandler(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var topicArn = _lambdaSettings.Value.TopicArn;
            if(topicArn.IsNullOrEmpty())
                throw new Exception("TopicArn not configured at Settings:TopicArn!");

            var batchItemFailures = new List<StreamsEventResponse.BatchItemFailure>();

            foreach (var record in dynamoDbEvent.Records)
            {
                try
                {
                    var newItem = Document.FromAttributeMap(record.Dynamodb.NewImage).ToJson();

                    _logger.LogDebug("{Record}", newItem);
                    var resp = await _snsService.PublishAsync(new PublishRequest
                    {
                        Message = $"Record: {newItem}",
                        Subject = "Alert Notification!",
                        TopicArn = topicArn,
                    }, CancellationToken.None);

                    var respStatusMsg = $"resp status: {resp.HttpStatusCode}";
                    _logger.LogDebug("{Message}", respStatusMsg);

                    if (resp.HttpStatusCode >= HttpStatusCode.BadRequest)
                        throw new Exception(respStatusMsg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "dynamoDB batch item failed: {SequenceNumber}", record.Dynamodb.SequenceNumber);
                    batchItemFailures.Add(new StreamsEventResponse.BatchItemFailure
                    {
                        ItemIdentifier = record.Dynamodb.SequenceNumber,
                    });
                }
            }

            return new StreamsEventResponse
            {
                BatchItemFailures = batchItemFailures,
            };
        }
    }
}
