using System.Threading;
using System.Threading.Tasks;
using Zocdoc.DependencyInjection;

namespace FaqChatbot.Web
{
    [RegisterService]
    public class ExamplesImpl: IExamples
    {
        public Task<ExampleEndpointResponse> ExampleEndpoint(CancellationToken cancellationToken)
        {
            return Task.FromResult(ExampleEndpointResponse.OK(new ExampleResponse
            {
                Stuff = "String Stuff"
            }));
        }

        
    }
}
