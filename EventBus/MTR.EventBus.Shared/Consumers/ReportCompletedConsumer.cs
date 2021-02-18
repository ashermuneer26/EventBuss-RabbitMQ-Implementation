﻿using System;
using Abp.Dependency;
using Abp.Runtime.Session;
using MassTransit;
using MTR.EventBus.Shared.Contracts;
using System.Threading.Tasks;
using AdvantageControl.Configuration;
using Castle.Core.Logging;
using Microsoft.Extensions.Configuration;

namespace MTR.EventBus.Shared.Consumers
{
    public class ReportCompletedConsumer : IConsumer<IReportCompletedContract>
    {
        private readonly IAbpSession _abpSession;
        private readonly IConfigurationRoot _appConfiguration;
        public ReportCompletedConsumer() {
            _abpSession = IocManager.Instance.Resolve<IAbpSession>();
            _appConfiguration = AppConfigurations.Get(AppContext.BaseDirectory);
        }

        public Task Consume(ConsumeContext<IReportCompletedContract> context)
        {
            using (_abpSession.Use(context.Message.TenantId, null))
            {
                using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
                {
                    logger.Object.InfoFormat(string.Format("{0} Submitted Service Report Id: ", context.Message.ReportId));
                }
            }

            return Task.CompletedTask;
        }
    }
}
