using System;
using System.Collections.Generic;
using System.Linq;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Tasks;
using Customization.ObjectModel;

namespace Customization.Tasks
{
    //TODO -Maintain naming conventions
    [SampleManagerTask("BarcodeLoginTask")]
    public class BarcodeLoginTask : DefaultFormTask
    {
        private FormBarcodeLogin m_Form;
        private BarcodeScan barcode;
        private EntityTemplateInternal entityTemplate;
        private Workflow entityWorkflow;
        private string scannedToField;
        private IList<IEntity> createdEntities;
        WorkflowHelper workflowHelper;

        public BarcodeLoginTask()
        {
            workflowHelper = new WorkflowHelper(Library);
        }

        protected override void MainFormCreated()
        {
            base.MainFormCreated();
            m_Form = (FormBarcodeLogin)MainForm;
        }

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            barcode = new BarcodeScan(EntityManager, Library, m_Form.ScanBox);

            m_Form.ScanBox.EditValueChanged += ScanBox_EditValueChanged;

            //Get Workflow & Entity Template
            ReadParameters();



        }

        private void ReadParameters()
        {
            var menuparams = Context.MenuItem.Get(MasterMenuPropertyNames.Parameters)?.ToString();

            if (menuparams is null)
            {
                throw new ArgumentNullException("Please Ensure that the following parameters are provided : EntityTemplateId, WorkflowGuid, WorkflowVersion");
            }

            var parameters = menuparams.Split(',');

            var entityTemplateId = String.Empty;
            var workflowGUID = String.Empty;
            var workflowVersion = String.Empty;

            if (parameters.Length > 3)
            {
                workflowVersion = parameters[3];
            }

            if (parameters.Length > 2)
            {
                entityTemplateId = parameters[1];
                workflowGUID = parameters[2];
                scannedToField = parameters[0];
            }
            else
            {
                throw new ArgumentException("Expected at least 3 parameters. Please separate each parameter by a comma");
            }

            entityWorkflow = EntityManager.GetWorkflowById(workflowGUID, workflowVersion);

            if (!(entityWorkflow.WorkflowType.PhraseId == PhraseWflowType.PhraseIdSAMPLE))
            {
                throw new ArgumentException($"Workflow {entityWorkflow.NameWithVersion()} is not a {PhraseWflowType.PhraseIdSAMPLE} Workflow!");
            }

            entityTemplate = EntityManager.GetEntityTemplateById(entityTemplateId);

            if (CheckFieldToScanExists(entityTemplate, scannedToField) == false)
            {
                throw new ArgumentException($"Could not find {scannedToField} in entityTemplate {entityTemplate.NameWithVersion()}");
            }

            //WorkflowHelper workflowHelper = new WorkflowHelper(Library);
            //createdEntities = workflowHelper.RunWorkflow(entityWorkflow, 1);
        }

        #region Custom Methods
        private bool CheckFieldToScanExists(EntityTemplateInternal entityTemplate, string scannedToField)
        {
            return entityTemplate.EntityTemplateProperties.Contains(scannedToField);
        }
        #endregion

        private void ScanBox_EditValueChanged(object sender, Thermo.SampleManager.Library.ClientControls.TextChangedEventArgs e)
        {
            barcode.Scan(sender, e);

            if (barcode.scannedValues.Count > 0)
            {
                var scannedValues = barcode.scannedValues;

                CreatePendingSamples(scannedValues);
            }
        }

        private void CreatePendingSamples(List<string> scannedValues)
        {
            IList<IEntity> newEntities = new List<IEntity>();
            //newEntities = CreateCopyOfEntities();

            newEntities = workflowHelper.RunWorkflow(entityWorkflow, 1);
            EntityManager.Transaction.Clear();

            foreach (var scannedValue in scannedValues)
            {
                foreach (var entity in newEntities)
                {
                    entity.Set(scannedToField, scannedValue); //Extract this, it's not a problem for the generic solution

                    //var pendingSample = EntityManager.CreateEntity<PendingSampleBase>();

                    //pendingSample.Status.PhraseId = PhraseUPenStat.PhraseIdSC;
                    //pendingSample.EntityTemplate = entityTemplate;
                    //(pendingSample as IEntity).StringToClob(PendingSamplePropertyNames.Clob, SerializeJSONUsingTemplateFields(entity, entityTemplate));
                    //pendingSample.Barcode = scannedValue;
                    //pendingSample.CreatedOn = DateTime.Now;
                    //pendingSample.CreatedBy = (PersonnelBase)Library.Environment.CurrentUser;

                    //EntityManager.Transaction.Add(pendingSample);

                    var scannedSample = EntityManager.CreateEntity<ScannedSampleBase>();

                    //TODO - Check config value and set the status
                    scannedSample.SetStatus(PhraseUPenStat.PhraseIdSC);
                    scannedSample.EntityTemplate = entityTemplate;
                    string serializedEntity = EntityTemplateHelper.SerializeJSONUsingEntityTemplate(entity, entityTemplate);
                    (scannedSample as IEntity).StringToClob(ScannedSamplePropertyNames.Clob, serializedEntity);
                    scannedSample.Barcode = scannedValue;
                    scannedSample.CreatedOn = DateTime.Now;
                    scannedSample.CreatedBy = (PersonnelBase)Library.Environment.CurrentUser;
                    scannedSample.WorkflowGuid = entityWorkflow.WorkflowGuid;

                    EntityManager.Transaction.Add(scannedSample);
                }
            }

            EntityManager.Commit();
            barcode.ResetScanData();
        }

        /// <summary>
        /// Copy the workflow entities without running them for each occurence
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private IList<IEntity> CreateCopyOfEntities()
        {
            var entities = createdEntities.ToList();
            return entities;
        }
    }
}
