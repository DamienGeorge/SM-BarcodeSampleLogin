using System;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.ObjectModel;
using System.Linq;

namespace Customization.Tasks
{
    [SampleManagerTask(nameof(ScheduleBackgroundTask), "WorkflowCallback")]
    public class ScheduleBackgroundTask : SampleManagerTask
    {
        protected override void SetupTask()
        {
            base.SetupTask();

            Logger.Error("Creating new Timerqueue Schedule Entry...");

            var parameters = Context.TaskParameters;

            Logger.Error(parameters.ToString());

            var timerqueue = EntityManager.CreateEntity("TIMERQUEUE") as Timerqueue;
            ScheduleTimerqueueEntry(timerqueue, parameters);

            LogTimerQueueEntry(timerqueue);
            EntityManager.Transaction.Add(timerqueue);

            Exit(true);

        }

        private void ScheduleTimerqueueEntry(Timerqueue timerqueue, string[] parameters)
        {

            timerqueue.ScheduleTask(parameters[0], String.Join(" ", parameters.Skip(1)), when: DateTime.Now.AddMinutes(1));
        }

        private void LogTimerQueueEntry(Timerqueue timerqueue)
        {
            Logger.Error("TimerQueue details..");

            Logger.Error(timerqueue.Task);
            Logger.Error(string.Join(",", timerqueue.TaskParams));
            Logger.Error(timerqueue.RunTime);
            Logger.Error(timerqueue.RecurrenceType);
            Logger.Error(timerqueue.RecurrenceData1);
            Logger.Error(timerqueue.Suspended);
            Logger.Error(timerqueue.UserName);
        }
    }
}
