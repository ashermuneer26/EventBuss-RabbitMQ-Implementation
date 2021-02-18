using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Runtime.Session;
using AdvantageControl.Configuration;
using BaseModule.Managers.BackgroundJobs;
using BaseModule.Managers.BackgroundJobs.Requestable;
using Castle.Core.Logging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using MTR.EventBus.Shared.Contracts;

namespace MTR.EventBus.Consumers
{
    public class UserEmailVerificationConsumer : IConsumer<IUserEmailVerificationContract>
    {
        private readonly IAbpSession _abpSession;
        private readonly IConfigurationRoot _appConfiguration;

        public UserEmailVerificationConsumer(IAbpSession abpSession)
        {
            _abpSession = abpSession;
            _appConfiguration = AppConfigurations.Get(AppContext.BaseDirectory);
        }

        public Task Consume(ConsumeContext<IUserEmailVerificationContract> context)
        {
            using (_abpSession.Use(context.Message.TenantId, null))
            {
                var backgroundJobsManager = IocManager.Instance.Resolve<IBackgroundJobsManager>();

                backgroundJobsManager.SendVerificationEmail(new EmailRequest
                {
                    BaseUrl = context.Message.BaseUrl,
                    EmailConfirmationCode = context.Message.EmailConfirmationCode,
                    FullName = context.Message.FullName,
                    Id = context.Message.UserId,
                    TenantId = context.Message.TenantId,
                    ToEmail = context.Message.ToEmail
                });
                using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
                {
                    context.Send<IEmailSentContract>(new EmailSentContract
                    {
                        TenantId = context.Message.TenantId,
                        UserId = context.Message.UserId,
                        Message = "Email Sent!"
                    });

                    logger.Object.InfoFormat(string.Format("{0} Sending the Verification Email to User Id:", context.Message.UserId));
                }
            }
            return Task.CompletedTask;
        }
    }
}
