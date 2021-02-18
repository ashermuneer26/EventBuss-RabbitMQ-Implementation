using Abp.Dependency;
using BaseModule.Schedulers;

namespace MTR.EventBus.Schedulers
{
    public class SchedulerRegisterar
    {
        public static void Register<T>()
            where T : IAdvantageControlScheduler
        {
            using (var s = IocManager.Instance.ResolveAsDisposable<T>()) {
                s.Object.Register().Wait();
            }
        }
    }
}
