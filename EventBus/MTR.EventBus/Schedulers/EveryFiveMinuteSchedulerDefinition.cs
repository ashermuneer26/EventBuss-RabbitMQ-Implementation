using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Quartz;
using Abp.Quartz.Configuration;
using BaseModule.Schedulers;
using MTR.EventBus.Listeners;
using Quartz;

namespace MTR.EventBus.Schedulers
{
    public class EveryFiveMinuteSchedulerDefinition : IAdvantageControlScheduler, ITransientDependency
    {
        private readonly IAbpQuartzConfiguration quartzConfiguration;
        private readonly IQuartzScheduleJobManager jobManager;

        public EveryFiveMinuteSchedulerDefinition(IAbpQuartzConfiguration quartzConfiguration,
               IQuartzScheduleJobManager jobManager
            )
        {
            this.quartzConfiguration = quartzConfiguration;
            this.jobManager = jobManager;
        }

        public JobKey GetJobKey => new JobKey("every-five-minute", "hourly");

        public async Task<bool> Exists()
        {
            return await quartzConfiguration.Scheduler.CheckExists(this.GetJobKey);
        }

        public async Task<bool> Register()
        {
            bool alreadyExists = await Exists();
            if (alreadyExists == false) {

                jobManager.ScheduleAsync<EveryFiveMinuteListener>(job =>
                {
                    job.WithIdentity(GetJobKey)
                        .WithDescription("Runs After every 5 minutes.");
                },
                trigger =>
                {
                    trigger.StartNow()
                    .WithSimpleSchedule( scheduler => {
                        scheduler.RepeatForever().WithIntervalInMinutes(5).Build();
                    })
                    .Build();
                }).Wait();
            }
            return true;
        }

        public async Task<Boolean> Remove()
        {
            bool isExists = await this.Exists();
            if (isExists) {
                this.quartzConfiguration.Scheduler.DeleteJob(this.GetJobKey).Wait();
            }
            return true;
        }
    }
}
