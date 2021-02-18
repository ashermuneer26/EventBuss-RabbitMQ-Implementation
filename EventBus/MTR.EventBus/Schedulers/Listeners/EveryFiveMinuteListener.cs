using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Quartz;
using Castle.Core.Logging;
using Quartz;

namespace MTR.EventBus.Listeners
{
    public class EveryFiveMinuteListener : JobBase, ITransientDependency
    {
        public EveryFiveMinuteListener()
        {
        }

        public override Task Execute(IJobExecutionContext context)
        {
            using (var Logger = IocManager.Instance.ResolveAsDisposable<ILogger>()) {
                Logger.Object.InfoFormat($"Every 5 minute Job Listener");
            }
            return Task.CompletedTask;
        }
    }
}
