using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using Zocdoc.DependencyInjection;
using Zocdoc.Monitoring;
using Zocdoc.Tracing.Providers;

namespace FaqChatbot.Cron
{
    [RegisterService(ServiceLifetime.Singleton)]
    public class CronTask : IHostedService
    {
        private ILogger<CronTask> _logger;
        private IMetricRecorder _metricRecorder;
        private IHostApplicationLifetime _applicationLifetime;

        public CronTask(ILogger<CronTask> logger, IMetricRecorder metricRecorder, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _metricRecorder = metricRecorder;
            _applicationLifetime = appLifetime;
        }

        private async Task DoTheThing(CancellationToken cancellationToken)
        {
            using var doTheThingActivity = ZdActivitySourceProvider.AppActivitySource.StartActivity("do-the-thing");

            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Starting to execute");

            await Task.Delay(1000, cancellationToken);

            _metricRecorder.Timer("execution_time", sw.ElapsedMilliseconds);
            _logger.LogInformation("Finished.");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var startActivity = ZdActivitySourceProvider.AppActivitySource.StartActivity("start-async");

            try
            {
                await DoTheThing(cancellationToken);
            }
            catch (TaskCanceledException e)
            {
                _logger.LogWarning($"task canceled: {e}");
            }
            finally
            {
                _applicationLifetime.StopApplication();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cleaning up");
            _metricRecorder.Flush();
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));
        }
    }
}
