using Quartz;
using System;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace NoAdsHere.Services.Penalties
{
    public static class JobQueue
    {
        private static IScheduler _scheduler;

        public static Task Install(IServiceProvider provider)
        {
            _scheduler = provider.GetService<IScheduler>();

            return Task.CompletedTask;
        }

        public static async Task QueueTrigger(IUserMessage message)
        {
            var job = JobBuilder.Create<DeleteMessageJob>()
                .StoreDurably()
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now.AddHours(1))
                .ForJob(job)
                .Build();
            trigger.JobDataMap["message"] = message;

            await _scheduler.ScheduleJob(job, trigger);
        }
    }

    public class DeleteMessageJob : IJob
    {
        private readonly Logger _logger = LogManager.GetLogger("AntiAds");

        public async Task Execute(IJobExecutionContext context)
        {
            var datamap = context.Trigger.JobDataMap;

            var message = (IUserMessage)datamap["message"];

            try
            {
                await message.DeleteAsync();
            }
            catch (Exception e)
            {
                _logger.Warn(e, $"Unable to delete message with ID: {message.Id}.");
            }
        }
    }
}