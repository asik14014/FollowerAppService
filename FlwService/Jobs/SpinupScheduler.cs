using Quartz;
using Quartz.Impl;
using System.Configuration;

namespace FlwService.Jobs
{
    public class SpinupScheduler
    {
        private static IScheduler _scheduler;
        public static void Start()
        {
            var minutes = 30;
            int.TryParse(ConfigurationManager.AppSettings["TimeInterval"], out minutes);
            _scheduler = StdSchedulerFactory.GetDefaultScheduler();
            _scheduler.Start();

            var job = JobBuilder.Create<Spinup>().Build();

            var startTime = new TimeOfDay(0, 0, 0);
            var endTime = new TimeOfDay(23, 59, 0);

            var trigger = TriggerBuilder.Create()       // создаем триггер
                .WithIdentity("trigger1", "group1")     // идентифицируем триггер с именем и группой
                .WithDailyTimeIntervalSchedule(x => x
                    .StartingDailyAt(startTime)
                    .EndingDailyAt(endTime)
                    .OnEveryDay()
                    .WithIntervalInMinutes(minutes))
                .StartNow()
                .Build();                               // создаем триггер

            _scheduler.ScheduleJob(job, trigger);        // начинаем выполнение работы

        }

        public static void Stop()
        {
            _scheduler.Shutdown();
        }
    }
}
