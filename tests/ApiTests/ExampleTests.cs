using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using FaqChatbot;
using ZocDoc.Tests.ApiTests;

namespace ApiTests
{
    public class TemplateAppTests
    {
        [SetUp]
        public void Setup() {
            ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => true;
        }

        [Test]
        public async Task VerifyServiceIsUp()
        {
            var result = await ApiTestHelpers.GetAsync<ExampleResponse>("/faq-chatbot/v1/an-example-endpoint");

            result.Stuff.Should().Be("String Stuff");
        }
        
    }
}
