using System.Reflection;
using Abp.Modules;
using AdvantageControl;
using MTR.EventBus.Shared.Extensions;

namespace MTR.EventBus.Shared
{
    [DependsOn(
        typeof(AdvantageControlCoreModule)
    )]
    public class EventBusSharedModule : AbpModule
    {
        public EventBusSharedModule() {

        }

        public override void PreInitialize()
        {
            IocManager.Register<EventBusModuleConfiguration>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}
