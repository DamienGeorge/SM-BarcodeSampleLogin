using Customization.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.SampleManager.Common.CommandLine;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Common.Workflow;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;
using Thermo.SampleManager.Server.Workflow;

namespace Customization.Tasks
{

    [SampleManagerTask(nameof(LoginScannedSamplesTask))]
    public class LoginScannedSamplesTask : SampleManagerTask, IBackgroundTask
    {

        [CommandLineSwitch("EntityTemplate", "Only create samples with this entityTemplate", false)]
        public string entityTemplateId { get; set; }
        //Debug
        protected override void SetupTask()
        {
            base.SetupTask();

            Launch();
        }

        public void Launch()
        {
            //DeleteCompletedSamples();

            ProcessNewSamples();
        }

        private void ProcessNewSamples()
        {
            IEntityCollection scannedSamples = GetSamplesToProcess();

            WorkflowHelper workflowHelper = new WorkflowHelper(Library);

            foreach (ScannedSampleBase scannedSample in scannedSamples)
            {
                //TODO - get workflow GUID?
                Workflow workflow = EntityManager.GetWorkflowById(scannedSample.WorkflowGuid);

                try
                {

                    IList<IEntity> createdSamples = workflowHelper.RunWorkflow(workflow, 1);

                    foreach (var sample in createdSamples)
                    {
                        MapSampleFromEntitytemplate(sample, scannedSample);
                    }
                }
                catch (Exception ex)
                {
                    //TODO - Catch the exception during login and add it to the scanned Sample
                    scannedSample.ErrorContent = ex.Message;
                    EntityManager.Transaction.Add(scannedSample);
                }
            }

            EntityManager.Commit();
        }

        private void MapSampleFromEntitytemplate(IEntity entity, ScannedSampleBase sample)
        {
            EntityTemplateHelper.DeserializeJSONUsingEntityTemplate(sample, (EntityTemplateInternal)sample.EntityTemplate, sample.ClobToString(PendingSamplePropertyNames.Clob), entity);

            EntityManager.Transaction.Add(entity);
        }

        /// <summary>
        /// Get all samples that haven't been logged in yet
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private IEntityCollection GetSamplesToProcess()
        {
            IQuery scannedSampleQuery = EntityManager.CreateQuery<ScannedSampleBase>();
            scannedSampleQuery.AddEquals(ScannedSamplePropertyNames.Status, PhraseUPenStat.PhraseIdSC);
            scannedSampleQuery.AddOrder(ScannedSamplePropertyNames.WorkflowGuid, ascending: true);
            scannedSampleQuery.AddOrder(ScannedSamplePropertyNames.EntityTemplate, ascending: true);
            scannedSampleQuery.AddOrder(ScannedSamplePropertyNames.CreatedOn, ascending: true);

            return EntityManager.Select(scannedSampleQuery);
        }

        /// <summary>
        /// Delete the Scanned Samples Marked for Deletion
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void DeleteCompletedSamples()
        {
            throw new NotImplementedException();
        }
    }
}
