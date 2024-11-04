using System;
using System.Drawing.Design;
using Thermo.SampleManager.Common.CommandLine;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;

namespace Customization.Tasks
{
    /// <summary>
    /// Background Task to Process the entries in Table ScannedEntities
    /// </summary>
    [SampleManagerTask(nameof(ScannedItemProcessorTask))]
    public class ScannedItemProcessorTask : SampleManagerTask, IBackgroundTask
    {
        private Personnel currentOperator;

        #region Overrides

        /// <summary>
        /// Entry Point to Debug task interactively
        /// </summary>
        protected override void SetupTask()
        {
            base.SetupTask();

            currentOperator = (Personnel)Library.Environment.CurrentUser;
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

            foreach (ScannedEntityBase scannedItem in scannedEntities)
            {
                try
                {
                    IEntityCollection entityCollection = new EntityCollection(TableNames.ScannedEntity)
                    {
                        scannedItem
                    };

                    //Transactions cannot span across tasks
                    var result = (IEntity)Library.Task.CreateTaskAndWait(scannedItem.TaskName, scannedItem.TaskParameters, entityCollection);

                    //EntityManager.Transaction.Add(result);
                    scannedItem.SetStatus(PhraseUPenStat.PhraseIdS);
                }
                catch (Exception ex)
                {
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
            IQuery scannedEntityQuery = EntityManager.CreateQuery<ScannedEntityBase>();
            scannedEntityQuery.AddEquals(ScannedEntityPropertyNames.Status, PhraseUPenStat.PhraseIdSC);

            //If processing on demand, only process the scanned entries for that user
            if (currentOperator != null)
            {
                scannedEntityQuery.AddEquals(ScannedEntityPropertyNames.ScannedBy, currentOperator);
            }

            scannedEntityQuery.AddOrder(ScannedEntityPropertyNames.ScannedOn, ascending: true);

            return EntityManager.Select(scannedEntityQuery);
        }

        #endregion
    }
}
