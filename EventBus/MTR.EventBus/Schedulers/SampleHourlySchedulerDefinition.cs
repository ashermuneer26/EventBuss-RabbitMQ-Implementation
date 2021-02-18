using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Quartz;
using Abp.Quartz.Configuration;
using BaseModule.Schedulers;
using MTR.EventBus.Schedulers.Listeners;
using Quartz;

namespace MTR.EventBus.Schedulers
{
    public class SampleHourlySchedulerDefinition : IAdvantageControlScheduler, ITransientDependency
    {
        private readonly IAbpQuartzConfiguration _quartzConfiguration;
        private readonly IQuartzScheduleJobManager _jobManager;

        public SampleHourlySchedulerDefinition(IAbpQuartzConfiguration quartzConfiguration,
            IQuartzScheduleJobManager jobManager
            )
        {
            _quartzConfiguration = quartzConfiguration;
            _jobManager = jobManager;
        }

        public JobKey GetJobKey => new JobKey("sample-hourly", "hourly");

        public async Task<bool> Exists()
        {
            return await _quartzConfiguration.Scheduler.CheckExists(this.GetJobKey);
        }

        public async Task<bool> Register()
        {
            var alreadyExists = await Exists();
            if (alreadyExists == false)
            {

                _jobManager.ScheduleAsync<SampleHourlyListener>(job =>
                {
                    job.WithIdentity(GetJobKey)
                        .WithDescription("Sample Scheduled Job that run after every 1 hour.");
                },
                trigger =>
                {
                    trigger.StartNow()
                    .WithSimpleSchedule(scheduler =>
                    {
                        scheduler.RepeatForever().WithIntervalInHours(1).Build();
                    })
                    .Build();
                }).Wait();
            }
            return true;
        }

        public async Task<bool> Remove()
        {
            var isExists = await Exists();
            if (isExists) {
                _quartzConfiguration.Scheduler.DeleteJob(GetJobKey).Wait();
            }
            return true;
        }
    }
}
