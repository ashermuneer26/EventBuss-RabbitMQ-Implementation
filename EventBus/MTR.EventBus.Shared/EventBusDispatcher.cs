using System;
using Abp.Dependency;
using Abp.Domain.Services;
using Castle.Core.Logging;
using MassTransit;
using MTR.EventBus.Shared.Contracts;

namespace MTR.EventBus.Shared
{
    public class EventBusDispatcher : DomainService
    {
        private readonly IBusControl eventBus;

        public EventBusDispatcher(IBusControl eventBus)
        {
            this.eventBus = eventBus;
        }


        public void Send<T>(T contract)
            where T : class, IContractable
        {
           this.eventBus.Send<T>(contract).ConfigureAwait(false);
        }


        public void Publish<T>(T contract)
            where T : class, IContractable
        {
            this.eventBus.Publish<T>(contract).ConfigureAwait(false);
        }

    }
}
