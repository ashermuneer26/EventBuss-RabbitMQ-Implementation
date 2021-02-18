using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Quartz;
using Abp.Runtime.Session;
using AdvantageControl.Configuration;
using BaseModule.Managers.BackgroundJobs;
using Castle.Core.Logging;
using Microsoft.Extensions.Configuration;
using MTR.EventBus.Shared;
using MTR.EventBus.Shared.Contracts;
using Quartz;

namespace MTR.EventBus.Schedulers.Listeners
{
    public class ReportScheduleListener : JobBase, ITransientDependency
    {
        private readonly IAbpSession _abpSession;
        private readonly IConfigurationRoot _appConfiguration;
        public ReportScheduleListener(IAbpSession abpSession)
        {
            _abpSession = abpSession;
            _appConfiguration = AppConfigurations.Get(AppContext.BaseDirectory);
        }
        public override Task Execute(IJobExecutionContext context)
        {

            var backgroundJobsManager = IocManager.Instance.Resolve<IBackgroundJobsManager>();

            using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
            {
                using (var eventDispatcher = IocManager.Instance.ResolveAsDisposable<EventBusDispatcher>())
                {
                    eventDispatcher.Object.Send<ILateReportsContract>(new LateReportsContract
                    {
                        TenantId = 0,
                        UserId = 0
                    });
                }
                logger.Object.InfoFormat(("Late Report Schedule Sent"));
            }

            return Task.CompletedTask;
        }
    }
}
