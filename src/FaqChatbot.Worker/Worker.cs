using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zocdoc.DependencyInjection;
using Zocdoc.Monitoring;
using Zocdoc.Tracing.Providers;

namespace FaqChatbot.Worker;

public interface ITicker
{
    public int DelayMilliseconds { get; set; }
    public int Ticks { get; set; }

    public void Tick();
}

[RegisterService(ServiceLifetime.Singleton)]
public class Ticker : ITicker
{
    private int _delayMilliseconds = 1000;
    public int DelayMilliseconds
    {
        get => _delayMilliseconds;
        set => _delayMilliseconds = value;
    }

    public int Ticks { get; set; }

    public void Tick()
    {
        Ticks += 1;
    }
}

public class TickerWorker : BackgroundService
{
    private readonly ITicker _ticker;
    private readonly ILogger<TickerWorker> _logger;
    private readonly IMetricRecorder _metricRecorder;

    public TickerWorker(ITicker ticker, ILogger<TickerWorker> logger, IMetricRecorder metricRecorder)
    {
        _ticker = ticker;
        _logger = logger;
        _metricRecorder = metricRecorder;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var activity = ZdActivitySourceProvider.AppActivitySource.StartActivity("worker-working");

            _ticker.Tick();
            _logger.LogInformation("Ticks: {Ticks}", _ticker.Ticks);
            _metricRecorder.Increment("ticks");
            await Task.Delay(TimeSpan.FromMilliseconds(_ticker.DelayMilliseconds), cancellationToken);
        }
    }
}
