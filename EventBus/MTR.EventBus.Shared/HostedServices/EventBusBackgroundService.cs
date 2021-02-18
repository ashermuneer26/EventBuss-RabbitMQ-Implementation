using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace MTR.EventBus.Shared.HostedServices
{
    public class EventBusBackgroundService : BackgroundService
    {
        private readonly IBusControl busControl;
        private ILogger Logger { get; set; }

        public EventBusBackgroundService(IBusControl busControl)
        {
            this.busControl = busControl;
            this.Logger = NullLogger.Instance;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.Info("Starting BusControl");
            return busControl.StartAsync(stoppingToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.Info("Stoping BusControl");
            return busControl.StopAsync(cancellationToken);
        }
    }
}
