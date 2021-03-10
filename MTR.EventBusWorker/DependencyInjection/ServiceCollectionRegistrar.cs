using Abp.Dependency;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTR.EventBus;
using SeriLogger;

namespace MTR.EventBusWorker.DependencyInjection
{
    public static class ServiceCollectionRegistrar
    {
        public static void Register(IIocManager iocManager, IConfigurationRoot appConfiguration)
        {
            var services = new ServiceCollection();
            LoggerRegistrar.Register<EventBusWorkerModule>(services);
            EventBusConsumerRegisterar.Register(services, appConfiguration);
            WindsorRegistrationHelper.CreateServiceProvider(iocManager.IocContainer, services);
        }
    }
}
