using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Runtime.Session;
using Castle.Core.Logging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using MTR.EventBus.Shared.Contracts;

namespace MTR.EventBus.Shared.Consumers
{
    public class EmailSentConsumer : IConsumer<IUserEmailVerificationContract>
    {
        private readonly IAbpSession _abpSession;
        private readonly IConfigurationRoot _appConfiguration;

        public EmailSentConsumer(IAbpSession abpSession)
        {
            _abpSession = abpSession;
            //_appConfiguration = AppConfigurations.Get(AppContext.BaseDirectory, null, false);
        }

        public Task Consume(ConsumeContext<IUserEmailVerificationContract> context)
        {
            using (_abpSession.Use(context.Message.TenantId, null))
            {
                using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
                {
                    logger.Object.InfoFormat(string.Format("{0} Email Sent to User Id: ", context.Message.UserId));
                }
            }

            return Task.CompletedTask;
        }
    }
}
