using System;
using Abp.Dependency;
using BaseModule.Configurations;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTR.EventBus.Shared.Consumers;
using MTR.EventBus.Shared.Contracts;
using MTR.EventBus.Shared.HostedServices;

namespace MTR.EventBus.Shared
{
    public static class EventBusPublisherRegisterar
    {
        public static IServiceCollection Register(IServiceCollection services, IConfigurationRoot appConfiguration)
        {
            var configurations = new RabbitMqConfigurations
            {
                Host = appConfiguration["RabbitMq:Host"],
                VirtualHost = appConfiguration["RabbitMq:VirtualHost"],
                UserName = appConfiguration["RabbitMq:UserName"],
                Password = appConfiguration["RabbitMq:Password"]
            };

            services.AddMassTransit(mt =>
            {
                mt.AddConsumer<ReportCompletedConsumer>().Endpoint(e => {
                    e.Name = EventBusQueue.ReportCompleted;
                    e.Temporary = false;
                });
               
               
                mt.AddBus(context => Bus.Factory.CreateUsingRabbitMq(sbc =>
                {
                    var queueHost = configurations.Host;
                    sbc.Host(new Uri(queueHost), configurations.VirtualHost, h =>
                    {
                        h.Username(configurations.UserName);
                        h.Password(configurations.Password);
                    });
                    sbc.UseInMemoryOutbox();

                    RegisterEndpointMap(queueHost);
                    sbc.ConfigureEndpoints(IocManager.Instance.IocContainer);
                 }));
            });

            services.AddHostedService<EventBusBackgroundService>();
            return services;
        }

        private static Uri GetQueueName(string hostUrl, string queueName)
        {
            return new Uri($"queue:{queueName}");
        }

        public static void RegisterEndpointMap(string queueHost)
        {
            
            EndpointConvention.Map<IUserEmailVerificationContract>(new Uri($"{queueHost}/{EventBusQueue.UserVerificationEmail}"));
           
        }

    }
}
