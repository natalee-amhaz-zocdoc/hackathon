using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using NUnit.Framework;
using FaqChatbot;
using ZocDoc.Tests.ApiTests;

namespace IntegrationTests
{
    public class LambdaTests
    {
        private IAmazonSQS _sqs;
        private ReceiveMessageRequest _request;

        [SetUp]
        public void Setup()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// assumes docker-compose web, localstack is up && ./scripts/setup-localstack.sh has been run successfully
        /// </summary>
        [Ignore("pending https://github.com/localstack/localstack/issues/6355 and https://github.com/localstack/localstack/issues/6369")]
        [Test]
        public async Task VerifyLambdaForwardsDynamoUpdate()
        {
            // arrange
            await GivenSqs();

            // act
            await ApiTestHelpers.PostAsync(
                "/api/restaurants",
                new CreateRestaurantRequest()
                {
                    Name = "JG Melon"
                },
                HttpStatusCode.Created
            );

            var result = await _sqs.ReceiveMessageAsync(_request);

            // assert
            result.Messages.Count.Should().Be(1);
            result.Messages.First().Body.Contains("JG Melon").Should().BeTrue();
        }

        private async Task GivenSqs()
        {
            var sqsUrl = System.Environment.GetEnvironmentVariable("ENDPOINT_URL") ?? "http://localhost:4566";
            var cred = new BasicAWSCredentials("localstack", "localstack");
            var config = new AmazonSQSConfig
            {
                ServiceURL = sqsUrl,
                AuthenticationRegion = "us-east-1",
                UseHttp = true,
            };
            _sqs = new AmazonSQSClient(cred, config);

            var fullQueueUrl = $"{sqsUrl}/000000000000/reservation-alerts-received";

            await _sqs.PurgeQueueAsync(new PurgeQueueRequest()
            {
                QueueUrl = fullQueueUrl,
            });

            _request = new ReceiveMessageRequest()
            {
                QueueUrl = fullQueueUrl,
            };
        }
    }
}
