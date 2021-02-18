using System;
using System.Collections.Specialized;
using Abp.Quartz.Configuration;
using Abp.Reflection.Extensions;
using BaseModule.Configurations;
using Microsoft.Extensions.Configuration;
using MTR.EventBus.Shared.Extensions;
using Quartz;
using Quartz.Impl;

namespace MTR.EventBus.Shared.Quartz
{
    public class AdvantageControlQuartzConfiguration : IAbpQuartzConfiguration
    {
        private readonly NameValueCollection props = new NameValueCollection();

        public AdvantageControlQuartzConfiguration(EventBusModuleConfiguration _configuration)
        {
            props.Add("quartz.scheduler.instanceName", "{string}");
            props.Add("quartz.scheduler.instanceId", "{int}");
            props.Add("quartz.threadPool.type", "Quartz.Simpl.SimpleThreadPool, Quartz");
            props.Add("quartz.threadPool.threadCount", "10");
            props.Add("quartz.jobStore.misfireThreshold", "60000");
            props.Add("quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz");

            props.Add("quartz.jobStore.useProperties", "true");
            props.Add("quartz.jobStore.dataSource", "default");
            props.Add("quartz.jobStore.tablePrefix", "QRTZ_");
            props.Add("quartz.jobStore.lockHandler.type", "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz");
            props.Add("quartz.dataSource.default.connectionString", _configuration.QuartzConnection);
            props.Add("quartz.dataSource.default.provider", "SqlServer");
            props.Add("quartz.serializer.type", "json");
            props.Add("quartz.jobStore.clustered", "true");
        }

        public IScheduler Scheduler => new StdSchedulerFactory(props).GetScheduler().Result;
    }
}
