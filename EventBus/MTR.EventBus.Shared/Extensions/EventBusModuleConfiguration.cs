using System;
using Abp.Configuration.Startup;
using Abp.Dependency;

namespace MTR.EventBus.Shared.Extensions
{
    public static class EventBusModuleConfigurationExtension
    {
        public static EventBusModuleConfiguration MyModule(this IModuleConfigurations moduleConfigurations)
        {
            return moduleConfigurations.AbpConfiguration.Get<EventBusModuleConfiguration>();
        }
    }

    /**
     *
     *
     */
    public class EventBusModuleConfiguration{
        public string QuartzConnection { get; set;}
    }
}
