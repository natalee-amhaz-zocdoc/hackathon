using Microsoft.AspNetCore.Hosting;
using FaqChatbot.Web;

namespace FaqChatbot.Fake
{
    public class FakeStartup : Startup
    {
        public FakeStartup(IWebHostEnvironment env) : base(env)
        {
            IsFake = true;
        }
    }
}
