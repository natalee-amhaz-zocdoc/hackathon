using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using FaqChatbot.Lambda;
using Zocdoc.DependencyInjection;
using Zocdoc.Lambda;

namespace UnitTests
{
    public class FakeTests
    {
        [Test]
        public void Startup_IFake_ServiceProvider_ShouldReturnFakeServices()
        {
            LambdaBase.UseFakeServices();
            var entryPoint = new EntryPoint();
            var testService = entryPoint.ServiceProvider.GetRequiredService<ITest>();
            testService.WhoAmI().Should().Be("Fake!");
        }

        private interface ITest
        {
            string WhoAmI();
        }

        [RegisterFakeService]
        internal class Fake : ITest
        {
            public string WhoAmI() => "Fake!";
        }

        [RegisterService]
        internal class Real : ITest
        {
            public string WhoAmI() => "Real!";
        }
    }
}
