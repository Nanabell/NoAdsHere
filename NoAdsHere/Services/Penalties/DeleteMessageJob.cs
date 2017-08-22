using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;

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

        public static async Task QueueTrigger(IUserMessage message, ILogger logger)
        {
            var job = JobBuilder.Create<DeleteMessageJob>()
                .StoreDurably()
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.Now.AddHours(1))
                .ForJob(job)
                .Build();
            trigger.JobDataMap["message"] = message;
            trigger.JobDataMap["logger"] = logger;

            await _scheduler.ScheduleJob(job, trigger);
        }
    }

    public class DeleteMessageJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var datamap = context.Trigger.JobDataMap;

            var message = (IUserMessage)datamap["message"];
            var logger = (ILogger)datamap["logger"];

            try
            {
                await message.DeleteAsync();
            }
            catch (Exception e)
            {
                logger.LogWarning(new EventId(400), e, $"Unable to delete message with ID: {message.Id}.");
            }
        }
    }
}