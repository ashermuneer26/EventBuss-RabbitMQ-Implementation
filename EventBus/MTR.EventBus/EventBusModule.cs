using System.Reflection;
using Abp.Modules;
using AdvantageControl;
using AdvantageControl.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MTR.EventBus.Shared;

namespace MTR.EventBus
{
    [DependsOn(
        typeof(AdvantageControlCoreModule),
        typeof(EventBusSharedModule),
        typeof(AdvantageControlEntityFrameworkModule)
    )]
    public class EventBusModule : AbpModule
    {

        public override void PreInitialize()
        {

        }

        public override void PostInitialize()
        {
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }

    }
}
