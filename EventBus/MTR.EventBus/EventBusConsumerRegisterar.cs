using System;
using Abp.Dependency;
using BaseModule.Configurations;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTR.EventBus.Consumers;
using MTR.EventBus.Shared;
using MTR.EventBus.Shared.Consumers;

namespace MTR.EventBus
{
    public static class EventBusConsumerRegisterar
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
            IocManager.Instance.IocContainer.AddMassTransit(mt =>
            {
               
                mt.AddConsumer<UserEmailVerificationConsumer>().Endpoint(e => {
                    e.Name = EventBusQueue.UserVerificationEmail;
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
                        EventBusPublisherRegisterar.RegisterEndpointMap(queueHost);
                        sbc.ConfigureEndpoints(IocManager.Instance.IocContainer);
                    })
                );
            });
            return services;
        }

    }
}
