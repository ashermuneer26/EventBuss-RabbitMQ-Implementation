using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Quartz;
using Abp.Quartz.Configuration;
using BaseModule.Schedulers;
using MTR.EventBus.Schedulers.Listeners;
using Quartz;

namespace MTR.EventBus.Schedulers
{
    public class ReportSchedulerDefination : IAdvantageControlScheduler, ITransientDependency
    {
        private readonly IAbpQuartzConfiguration _quartzConfiguration;
        private readonly IQuartzScheduleJobManager _jobManager;

        public ReportSchedulerDefination(IAbpQuartzConfiguration quartzConfiguration,
            IQuartzScheduleJobManager jobManager)
        {
            _quartzConfiguration = quartzConfiguration;
            _jobManager = jobManager;
        }

        public JobKey GetJobKey => new JobKey("sample-instant", "instantly");
        public async Task<bool> Register()
        {
            var alreadyExists = await Exists();
            if (alreadyExists == false)
            {

                _jobManager.ScheduleAsync<ReportScheduleListener>(job =>
                    {
                        job.WithIdentity(GetJobKey)
                            .WithDescription("Sample Scheduled Job that run after every Month.");
                    },
                    trigger =>
                    {
                        trigger.StartNow()
                            .WithSimpleSchedule(scheduler =>
                            {
                                scheduler.RepeatForever().WithIntervalInHours(720).Build();
                            })
                            .Build();
                    }).Wait();
            }

            return true;
        }

        public async Task<bool> Remove()
        {
            var isExists = await Exists();
            if (isExists)
            {
                _quartzConfiguration.Scheduler.DeleteJob(GetJobKey).Wait();
            }
            return true;
        }

        public async Task<bool> Exists()
        {
            return await _quartzConfiguration.Scheduler.CheckExists(this.GetJobKey);
        }
    }
}
