using System;
using Thermo.SampleManager.Common.CommandLine;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Server;

namespace Customization.Tasks
{
    /// <summary>
    /// Background Task to Process the entries in Table ScannedEntities
    /// </summary>
    [SampleManagerTask(nameof(ScannedItemProcessorTask))]
    public class ScannedItemProcessorTask : SampleManagerTask, IBackgroundTask
    {
        [CommandLineSwitch("operatorName", "Choose Operator to selectively run the task for", false)]
        public string CurrentOperator { get; set; }

        #region Overrides

        /// <summary>
        /// Entry Point to Debug task interactively
        /// </summary>
        protected override void SetupTask()
        {
            base.SetupTask();

            if (Library.Environment.IsBackground() == false)
            {
                CurrentOperator = Library.Environment.CurrentUser.Name;
                Launch();
            }

        }
        #endregion

        #region Custom Methods
        /// <summary>
        /// Entry point for Background Task
        /// </summary>
        public void Launch()
        {
            Logger.Error($"{DateTime.Now} : Running Scanned Processor Task...");
            var startTime = DateTime.Now;
            ProcessNewEntries();

            var timeTaken = DateTime.Now - startTime;
            Logger.Error($"{DateTime.Now} : Completed Running Scanned Processor Task in {timeTaken.TotalSeconds} s");
        }

        /// <summary>
        /// Process any new Scanned Entities
        /// </summary>
        private void ProcessNewEntries()
        {
            IEntityCollection scannedEntities = GetEntitiesToProcess();

            Logger.Error($"Found {scannedEntities.Count} entities to process");
            foreach (ScannedEntityBase scannedItem in scannedEntities)
            {
                Logger.Error($"{DateTime.Now} : Processing {scannedItem.Name}...");
                try
                {
                    IEntityCollection entityCollection = new EntityCollection(TableNames.ScannedEntity)
                    {
                        scannedItem
                    };

                    //Transactions cannot span across tasks
                    var result = (IEntity)Library.Task.CreateTaskAndWait(scannedItem.TaskName, scannedItem.TaskParameters, string.Empty, ScannedEntityBase.StructureTableName, entityCollection);

                    //EntityManager.Transaction.Add(result);
                    scannedItem.SetStatus(PhraseUPenStat.PhraseIdS);
                    Logger.Error($"{DateTime.Now} : Completed Processing {scannedItem.Name} Successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    scannedItem.ErrorContent = ex.Message;
                    scannedItem.SetStatus(PhraseUPenStat.PhraseIdE);
                }

                EntityManager.Transaction.Add(scannedItem);
            }

            EntityManager.Commit();
        }

        /// <summary>
        /// Get all samples that haven't been logged in yet
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private IEntityCollection GetEntitiesToProcess()
        {
            Logger.Error("Getting Entitites To Process...");

            IQuery scannedEntityQuery = EntityManager.CreateQuery<ScannedEntityBase>();
            scannedEntityQuery.AddEquals(ScannedEntityPropertyNames.Status, PhraseUPenStat.PhraseIdSC);

            //If processing on demand, only process the scanned entries for that user
            if (CurrentOperator != null)
            {
                Logger.Error($"Using Operator : {CurrentOperator}");
                scannedEntityQuery.AddEquals(ScannedEntityPropertyNames.ScannedBy, CurrentOperator);
            }

            scannedEntityQuery.AddOrder(ScannedEntityPropertyNames.ScannedOn, ascending: true);

            return EntityManager.Select(scannedEntityQuery);
        }

        #endregion
    }
}