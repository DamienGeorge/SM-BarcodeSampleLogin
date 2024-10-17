using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Tasks;
using System.Linq;
using Thermo.LabExecution.Tasks;
using Thermo.SampleManager.ObjectModel.ImportHelpers;
using Thermo.Framework.Core;

namespace Customization.Tasks
{
    //TODO -Maintain naming conventions
    [SampleManagerTask("BarcodeLoginTask_old")]
    public class BarcodeLoginTask_old : DefaultFormTask
    {
        private FormBarcodeLogin m_Form;
        private BarcodeLogin barcode;
        private EntityTemplateInternal entityTemplate;
        private Workflow entityWorkflow;
        private string scannedToField;
        private IList<IEntity> createdEntities;

        public BarcodeLoginTask_old()
        {

        }

        protected override void MainFormCreated()
        {
            base.MainFormCreated();
            m_Form = (FormBarcodeLogin)MainForm;
        }

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            barcode = new BarcodeLogin(EntityManager, Library, m_Form.ScanBox);

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

            entityWorkflow = GetWorkflowById(workflowGUID, workflowVersion);

            if (!(entityWorkflow.WorkflowType.PhraseId == PhraseWflowType.PhraseIdSAMPLE))
            {
                throw new ArgumentException($"Workflow {entityWorkflow.NameWithVersion()} is not a {PhraseWflowType.PhraseIdSAMPLE} Workflow!");
            }

            entityTemplate = GetEntityTemplateById(entityTemplateId);

            if (CheckFieldToScanExists(entityTemplate, scannedToField) == false)
            {
                throw new ArgumentException($"Could not find {scannedToField} in entityTemplate {entityTemplate.NameWithVersion()}");
            }

            WorkflowHelper workflowHelper = new WorkflowHelper(Library);
            createdEntities = workflowHelper.RunWorkflow(entityWorkflow, 1);
        }

        #region Custom Methods

        private EntityTemplateInternal GetEntityTemplateById(string entityTemplateId)
        {
            return EntityManager.SelectLatestVersion<EntityTemplateInternal>(entityTemplateId) ?? throw new NullReferenceException($"Could not find EntityTemplate with id : {entityTemplateId}");
        }

        private Workflow GetWorkflowById(string workflowGUID, string workflowVersion)
        {
            if (String.IsNullOrEmpty(workflowVersion))
            {
                return EntityManager.SelectLatestVersion<Workflow>(new Identity(workflowGUID)) ?? throw new NullReferenceException($"Could not find an active workflow with id : {workflowGUID}");
            }
            else
            {
                return EntityManager.Select<Workflow>(new Identity(workflowGUID, workflowVersion)) ?? throw new NullReferenceException($"Could not find workflow with id : {workflowGUID} and version : {workflowVersion}");
            }
        }
        private bool CheckFieldToScanExists(EntityTemplateInternal entityTemplate, string scannedToField)
        {
            return entityTemplate.EntityTemplateProperties.Contains(scannedToField);
        }
        #endregion

        private void ScanBox_EditValueChanged(object sender, Thermo.SampleManager.Library.ClientControls.TextChangedEventArgs e)
        {
            barcode.Scan(sender, e);

            if (barcode.isScanned)
            {
                var scannedValues = barcode.scannedValues;

                CreatePendingSamples(scannedValues);
            }
        }

        private void CreatePendingSamples(List<string> scannedValues)
        {
            IList<IEntity> newEntities = new List<IEntity>();
            newEntities = CreateCopyOfEntities();
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

                    scannedSample.SetStatus(PhraseUPenStat.PhraseIdSC);
                    scannedSample.EntityTemplate = entityTemplate;
                    (scannedSample as IEntity).StringToClob(ScannedSamplePropertyNames.Clob, EntityTemplateHelper.SerializeJSONUsingEntityTemplate(entity, entityTemplate));
                    scannedSample.Barcode = scannedValue;
                    scannedSample.CreatedOn = DateTime.Now;
                    scannedSample.CreatedBy = (PersonnelBase)Library.Environment.CurrentUser;

                    EntityManager.Transaction.Add(scannedSample);
                }
            }

            EntityManager.Commit();
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
