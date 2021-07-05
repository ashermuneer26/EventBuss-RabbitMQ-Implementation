using System;
using Abp.AspNetCore;
using Abp.Dependency;
using Abp.Modules;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Castle.Facilities.Logging;
using Castle.Services.Logging.SerilogIntegration;
using Castle.Windsor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace SeriLogger
{
    public static class LoggerRegistrar
    {
        public static IServiceProvider Register<M>(IServiceCollection services) where M : AbpModule{
           return services.AddAbp<M>(options =>
           {
               var loggerConfig = new ConfigurationBuilder()
                    .AddJsonFile("serilog.json", optional: false, reloadOnChange: true)
                    .Build();

               var loggerInstance = new LoggerConfiguration()
                   .ReadFrom.Configuration(loggerConfig)
                   .Enrich.FromLogContext()
                   .Enrich.WithProperty("ApplicationName", typeof(M).Assembly.GetName().Name)
                   .CreateLogger();

               options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                   f => f.LogUsing(new SerilogFactory(loggerInstance))
               );
           });
        }

        public static LoggerConfiguration Register(LoggerConfiguration loggerConfiguration)
        {
            var loggerConfig = new ConfigurationBuilder()
                   .AddJsonFile("serilog.json", optional: false, reloadOnChange: true)
                   .Build();

            loggerConfiguration
            .ReadFrom.Configuration(loggerConfig)
            .Enrich.FromLogContext();
            return loggerConfiguration;
        }


        public static void Register(IWindsorContainer container, String ApplicationName)
        {
            var loggerConfig = new ConfigurationBuilder()
                     .AddJsonFile("serilog.json", optional: false, reloadOnChange: true)
                     .Build();
            var loggerInstance = new LoggerConfiguration()
                .ReadFrom.Configuration(loggerConfig)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", ApplicationName)
                .CreateLogger();

            container.AddFacility<LoggingFacility>(
                f => f.LogUsing(new SerilogFactory(loggerInstance))
            );
        }
    }
}
