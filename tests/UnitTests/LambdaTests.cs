using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using FaqChatbot.Lambda;
using Zocdoc.DependencyInjection;
using Zocdoc.Lambda;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace UnitTests
{
    public class EntryPointTests
    {
        [Test]
        public async Task Verify_EntryPoint_Sets_Up_Correctly()
        {
            // use fake IHandler
            LambdaBase.UseFakeServices();
            var entryPoint = new EntryPoint();
            await entryPoint.DdbEventStreamHandler(null, new TestLambdaContext());
        }

        [RegisterFakeService]
        internal class Handy : IHandler
        {
            public async Task<StreamsEventResponse> FunctionHandler(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
            {
                await Task.Run(() => context.Logger.Log("Hello!"));
                return new StreamsEventResponse();
            }
        }
    }

    public class HandlerTests
    {
        private IOptions<LambdaSettings> _lambdaSettings;
        private IAmazonSimpleNotificationService _sns;
        private DynamoDBEvent _dynamoDbEvent;
        private ILambdaContext _lambdaContext;
        private Mock<ILogger<Handler>> _logger;

        [Test]
        public async Task Verify_Handler_Processes_Correctly()
        {
            GivenLambdaSettings();
            GivenLogger();
            GivenSnsService();
            GivenDynamoDbEvent();
            GivenLambdaContext();

            var handler = new Handler(_lambdaSettings, _logger.Object, _sns);
            var resp = await handler.FunctionHandler(_dynamoDbEvent, _lambdaContext);

            resp.BatchItemFailures.Count.Should().Be(0);

            VerifyLoggerCalled();
        }

        [Test]
        public async Task Verify_Handler_Batch_Item_Failures()
        {
            GivenLambdaSettings();
            GivenLogger();
            GivenSnsService(statusCode: HttpStatusCode.InternalServerError);
            GivenDynamoDbEvent();
            GivenLambdaContext();

            var handler = new Handler(_lambdaSettings, _logger.Object, _sns);
            var resp = await handler.FunctionHandler(_dynamoDbEvent, _lambdaContext);

            resp.BatchItemFailures.Count.Should().BePositive();
        }

        #region Setup

        private void GivenLambdaSettings()
        {
            _lambdaSettings = Options.Create(new LambdaSettings()
            {
                TopicArn = "fake:arn",
            });
        }

        private void GivenDynamoDbEvent()
        {
            _dynamoDbEvent = new DynamoDBEvent
            {
                Records = new List<DynamoDBEvent.DynamodbStreamRecord>
                {
                    new DynamoDBEvent.DynamodbStreamRecord
                    {
                        AwsRegion = "us-west-2",
                        Dynamodb = new StreamRecord
                        {
                            ApproximateCreationDateTime = DateTime.Now,
                            Keys = new Dictionary<string, AttributeValue>
                            {
                                {"id", new AttributeValue {S = "MyId"}}
                            },
                            NewImage = new Dictionary<string, AttributeValue>
                            {
                                {"Name", new AttributeValue {S = "JG Melon"}},
                            },
                            StreamViewType = StreamViewType.NEW_IMAGE
                        }
                    }
                }
            };
        }

        private void GivenSnsService(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var mockSns = new Mock<IAmazonSimpleNotificationService>();
            mockSns
                .Setup(x =>
                    x.PublishAsync(It.IsAny<PublishRequest>(), CancellationToken.None))
                .ReturnsAsync(new PublishResponse{ HttpStatusCode = statusCode});
            _sns = mockSns.Object;
        }

        private void GivenLambdaContext()
        {
            _lambdaContext = new Mock<ILambdaContext>().Object;
        }

        private void GivenLogger()
        {
            _logger = new Mock<ILogger<Handler>>();
        }

        #endregion

        #region Verify

        private void VerifyLoggerCalled()
        {
            var recordJson = Document.FromAttributeMap(_dynamoDbEvent.Records.First().Dynamodb.NewImage).ToJson();
            _logger.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Debug),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == recordJson),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
            ));
        }

        #endregion
    }
}
