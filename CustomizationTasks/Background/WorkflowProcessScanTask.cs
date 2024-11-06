using Thermo.SampleManager.Library;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server.Workflow;
using Customization.ObjectModel;
using Thermo.SampleManager.Library.EntityDefinition;
using System;
using Thermo.SampleManager.Common.Workflow;

namespace Customization.Tasks
{
    [SampleManagerTask(nameof(WorkflowProcessScanTask))]
    public class WorkflowProcessScanTask : SampleManagerTask
    {
        /// <summary>
        /// Entry Point
        /// </summary>
        protected override void SetupTask()
        {
            base.SetupTask();

            ScannedEntityBase scannedEntity = (ScannedEntityBase)Context.SelectedItems[0];

            string workflowGuid = Context.TaskParameters[0];
            Workflow workflow = EntityManager.GetWorkflowById(workflowGuid);

            IWorkflowPropertyBag propertyBag = new WorkflowPropertyBag();
            AddParametersToWorkflowPropertyBag(propertyBag, scannedEntity);

            try
            {
                Library.Workflow.Perform(workflow, propertyBag);

                if (propertyBag.HasErrors)
                {
                    foreach (WorkflowError error in propertyBag.Errors)
                    {
                        Logger.Info(error.Message);
                    }
                }

                foreach (var propertyBagEntity in propertyBag.Entities)
                {
                    foreach (var entity in propertyBagEntity.Value)
                    {
                        EntityManager.Transaction.Add(entity);
                    }
                }

                EntityManager.Commit();
                Exit();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error(ex.InnerException);

                throw;
            }

        }

        /// <summary>
        /// Set Parameters to use from workflow
        /// </summary>
        /// <param name="propertyBag"></param>
        /// <param name="scannedEntity"></param>
        private void AddParametersToWorkflowPropertyBag(IWorkflowPropertyBag propertyBag, ScannedEntityBase scannedEntity)
        {
            for (int i = 0; i < Context.TaskParameters.Length; i++)
            {
                if (Context.TaskParameters[i] == Context.TaskParameters[0])
                {
                    continue;
                }

                propertyBag.Set($"$Param{i}", Context.TaskParameters[i]);
            }

            //TODO - Allow scanned entity to be separated by record separator
            propertyBag.Set("$Barcode", scannedEntity.ScannedText);
            propertyBag.Set("$ScannedBy", scannedEntity.ScannedBy);
            propertyBag.Set("$ScannedOn", scannedEntity.ScannedOn.Value.ToString("s"));
        }
    }
}

