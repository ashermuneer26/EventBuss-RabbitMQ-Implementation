using System.Reflection;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using AdvantageControl.MultiTenancy;
using Castle.Core.Logging;
using Microsoft.Extensions.Hosting;
using MTR.EventBus.Shared.Contracts;

namespace MTR.EventBus.Shared.HostedServices
{
    [UnitOfWork]
    public class HeartbeatHostedService : BackgroundService
    {
        private readonly EventBusDispatcher eventBusDispatcher;
        public HeartbeatHostedService(EventBusDispatcher eventBusDispatcher)
        {
            this.eventBusDispatcher = eventBusDispatcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1 * 60 * 100, stoppingToken);
                
                using (var Logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
                {

                    /* Commented the code for testing purpose to check complete cycle of Service Report Generation */
                    using (var EventDispatcher = IocManager.Instance.ResolveAsDisposable<EventBusDispatcher>())
                    {
                        EventDispatcher.Object.Send<IHeartbeatContract>(new HeartbeatContract()
                        {
                            ApplicationId = Assembly.GetExecutingAssembly().GetName().Name,
                            Message = "Heartbeat"
                        });
                    }

                    /*
                    using (var tenantRepo = IocManager.Instance.ResolveAsDisposable<IRepository<Tenant, int>>())
                    {
                        this.eventBusDispatcher.Send<IServiceReportContract>(new ServiceReportContract()
                        {
                            TenantId = 2,
                            ReportId = 1003
                        });
                    }*/
                }
            }
        }
    }
}
