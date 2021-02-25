using Abp.AutoMapper;
using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.MicroKernel.Registration;
using MTR.EventBus.Shared;

namespace AdvantageControl
{
    [DependsOn(
        typeof(AdvantageControlCoreModule), 
        typeof(AbpAutoMapperModule),
        typeof(EventBusSharedModule)
    )]
    public class AdvantageControlApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
            Configuration.ReplaceService(
                typeof(IEventBus),
                () => IocManager.IocContainer.Register(
                    Component.For<IEventBus>().Instance(NullEventBus.Instance)
                )
            );
        }

        public override void Initialize()
        {
            var thisAssembly = typeof(AdvantageControlApplicationModule).GetAssembly();

            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)                 
            );
        }
    }
}
