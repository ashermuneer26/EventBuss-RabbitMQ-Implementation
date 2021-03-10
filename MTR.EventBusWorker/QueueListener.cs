using Abp.Domain.Services;
using Abp.Quartz;
using Abp.Quartz.Configuration;
using MassTransit;
using MTR.EventBus.Shared.Extensions;

namespace MTR.EventBusWorker
{
    public class QueueListener : DomainService
    {
        private readonly IBusControl busControl;
        private readonly IQuartzScheduleJobManager jobManager;
        private readonly IAbpQuartzConfiguration quartzConfiguration;
        private readonly EventBusModuleConfiguration eventBusConfiguration;

        public QueueListener(IBusControl busControl,
            IQuartzScheduleJobManager jobManager,
            IAbpQuartzConfiguration quartzConfiguration,
            EventBusModuleConfiguration eventBusConfiguration)
        {
            this.busControl = busControl;
            this.jobManager = jobManager;
            this.quartzConfiguration = quartzConfiguration;
            this.eventBusConfiguration = eventBusConfiguration;
        }


        public void Start()
        {
            Logger.InfoFormat($"Starting EventBus Listener - {this.eventBusConfiguration.QuartzConnection} - {quartzConfiguration.ToString()}");
            busControl.StartAsync().Wait();
            jobManager.Start();
        }

        public void Stop()
        {
            Logger.DebugFormat("Stoping EventBus Listener");
            busControl.Stop();
            jobManager.WaitToStop();
        }
    }
}
