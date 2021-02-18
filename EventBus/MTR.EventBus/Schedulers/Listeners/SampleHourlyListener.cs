using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Quartz;
using Castle.Core.Logging;
using Quartz;

namespace MTR.EventBus.Schedulers.Listeners
{
    public class SampleHourlyListener: JobBase, ITransientDependency
    {
        public SampleHourlyListener()
        {
        }

        public override Task Execute(IJobExecutionContext context)
        {
            using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>()) {


            }
            return Task.CompletedTask;
        }
    }
}
