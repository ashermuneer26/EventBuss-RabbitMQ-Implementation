using System.Reflection;
using Abp.Modules;
using MTR.EventBus.Shared.Extensions;

namespace MTR.EventBus.Shared
{
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
