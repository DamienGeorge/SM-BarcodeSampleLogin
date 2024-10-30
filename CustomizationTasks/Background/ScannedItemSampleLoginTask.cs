using Customization.ObjectModel;
using System;
using System.Collections.Generic;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.ObjectModel;

namespace Customization.Tasks
{
    /// <summary>
    /// Specialized Task to Create Entity for the scanned text
    /// Called from ScannedItemProcessorTask
    /// </summary>
    [SampleManagerTask(nameof(ScannedItemSampleLoginTask))]
    public class ScannedItemSampleLoginTask : SampleManagerTask
    {
        #region Global Variables
        private string m_InputValue;
        private string workflowId;
        private string scannedFieldName;
        private string jobName;
        JobHeaderBase jobHeader;
        #endregion

        #region Overrides
        /// <summary>
        /// Entry Point for task
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        protected override void SetupTask()
        {
            base.SetupTask();

            m_InputValue = Context.TaskParameterString;
            ScannedEntityBase scannedEntity = (ScannedEntityBase)Context.SelectedItems[0];

            var parameters = m_InputValue.Split(',');

            if (parameters.Length > 2)
            {
                jobName = parameters[2];
                jobHeader = EntityManager.Select<JobHeaderBase>(jobName);
            }

            if (parameters.Length > 0)
            {
                workflowId = parameters[0];
                scannedFieldName = parameters[1];
            }
            else
            {
                throw new ArgumentException("Missing Name of field to which to assign ScannedText!");
            }


            Workflow workflow = EntityManager.GetWorkflowById(workflowId);


            try
            {
                Run(workflow, scannedEntity);

                Exit();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Custom Methods
        /// <summary>
        /// Run the Workflow for the Scanned entity
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="scannedEntity"></param>
        /// <returns></returns>
        public void Run(Workflow workflow, ScannedEntityBase scannedEntity)
        {
            try
            {
                WorkflowHelper workflowHelper = new WorkflowHelper(Library);

                IList<IEntity> createdSamples = workflowHelper.RunWorkflow(workflow, 1);
                EntityManager.Transaction.Clear();

                workflowHelper.SetProcessDeferred(createdSamples);


                foreach (SampleBase sample in createdSamples)
                {
                    ISchemaField schemaField = sample.FindSchemaField(scannedFieldName);

                    sample.SetFieldByType(scannedFieldName, schemaField, scannedEntity.ScannedText);
                    sample.UScannedBy = scannedEntity.ScannedBy;
                    sample.UScannedOn = scannedEntity.ScannedOn;

                    if (jobHeader != null)
                    {
                        sample.JobName = jobHeader;
                    }

                    EntityManager.Transaction.Add(sample);
                }
                EntityManager.Commit();

                workflowHelper.ProcessDeferredTriggers(EntityManager);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error(ex.InnerException);

                throw ex;
            }
        }

        /// <summary>
        /// Delete the Scanned Samples Marked for Deletion
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void DeleteCompletedSamples()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
