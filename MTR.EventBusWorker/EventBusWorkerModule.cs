using Abp.Modules;
using Abp.Reflection.Extensions;
using Microsoft.Extensions.Configuration;
using MTR.EventBus;
using BaseModule.Configurations;
using Abp.Events.Bus;
using Castle.MicroKernel.Registration;
using Abp.AutoMapper;
using MTR.EventBusWorker.DependencyInjection;
using Abp.Quartz.Configuration;
using Abp.Quartz;
using MTR.EventBus.Shared.Quartz;
using MTR.EventBus.Shared;
using Automatonymous;
using MTR.EventBus.Shared.Extensions;

namespace MTR.EventBusWorker
{
    [DependsOn(
        typeof(SeriLogger.SeriLoggerModule),
        typeof(AbpAutoMapperModule),
        typeof(AbpQuartzModule),
        typeof(EventBusModule),
        typeof(EventBusSharedModule)
    )]
    public class EventBusWorkerModule : AbpModule
    {
        private readonly IConfigurationRoot _appConfiguration;

        public EventBusWorkerModule() {
            _appConfiguration = AppConfigurations.Get(
                typeof(EventBusWorkerModule).GetAssembly().GetDirectoryPathOrNull()
            );
        }

        public override void PreInitialize()
        {
            Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
                "Default"
            );

            Configuration.Get<EventBusModuleConfiguration>().QuartzConnection = _appConfiguration.GetConnectionString(
                "Quartz"
            );

            Configuration.BackgroundJobs.IsJobExecutionEnabled = true;
            IocManager.IocContainer.Register(Component
                .For<IAbpQuartzConfiguration>()
                .ImplementedBy<AdvantageControlQuartzConfiguration>()
                .IsDefault());
            Configuration.Modules.AbpQuartz().Scheduler.JobFactory = new AbpQuartzJobFactory(IocManager);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(EventBusWorkerModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            Configuration.BackgroundJobs.IsJobExecutionEnabled = true;
            ServiceCollectionRegistrar.Register(IocManager, _appConfiguration);
        }
    }

}
