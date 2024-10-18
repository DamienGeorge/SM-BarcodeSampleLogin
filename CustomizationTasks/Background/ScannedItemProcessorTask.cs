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

        #region Overrides

        /// <summary>
        /// Entry Point to Debug task interactively
        /// </summary>
        protected override void SetupTask()
        {
            base.SetupTask();

            Launch();
        }
        #endregion

        #region Custom Methods
        /// <summary>
        /// Entry point for Background Task
        /// </summary>
        public void Launch()
        {
            ProcessNewEntries();
        }

        /// <summary>
        /// Process any new Scanned Entities
        /// </summary>
        private void ProcessNewEntries()
        {
            IEntityCollection scannedEntities = GetEntitiesToProcess();

            WorkflowHelper workflowHelper = new WorkflowHelper(Library);

            foreach (ScannedEntityBase scannedItem in scannedEntities)
            {
                try
                {
                    IEntityCollection entityCollection = new EntityCollection(TableNames.ScannedEntity)
                    {
                        scannedItem
                    };

                    //Transactions cannot span across tasks
                    var result = Library.Task.CreateTaskAndWait(scannedItem.TaskName, scannedItem.TaskParameters, entityCollection);


                    scannedItem.SetStatus(PhraseUPenStat.PhraseIdS);
                }
                catch (Exception ex)
                {
                    scannedItem.ErrorContent = ex.Message;
                    scannedItem.SetStatus(PhraseUPenStat.PhraseIdS);
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
            IQuery scannedSampleQuery = EntityManager.CreateQuery<ScannedEntityBase>();
            scannedSampleQuery.AddEquals(ScannedEntityPropertyNames.Status, PhraseUPenStat.PhraseIdSC);
            scannedSampleQuery.AddOrder(ScannedEntityPropertyNames.ScannedOn, ascending: true);

            return EntityManager.Select(scannedSampleQuery);
        }

        #endregion
    }
}
