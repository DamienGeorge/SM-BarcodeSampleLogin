using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Core.Exceptions;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.ClientControls.Browse;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;
using Thermo.SampleManager.Tasks;
using static System.Windows.Forms.AxHost;
using Timer = System.Timers.Timer;
using Environment = System.Environment;
using System.Drawing;
using Thermo.SampleManager.Common.Workflow;
using Thermo.SampleManager.Server.Workflow;
using Customization.Tasks;
using System.Linq;

namespace ISLAND.Tasks
{
    [SampleManagerTask("UPendingSampleModifyTask")]
    public class UPendingSampleModifyTask : DefaultFormTask
    {
        #region Member variables
        private EntityTemplateInternal entityTemplate;
        private FormSampleReceipt m_Form;
        private UnboundGrid m_UnboundGrid;
        string scannedToField = String.Empty;
        private string m_Title;

        private IQuery m_CriteriaQuery;
        private bool m_InitialisingCriteria;

        /// <summary>Indicate that a fatal error occurred - also used as a quick way out</summary>
        protected bool FatalErrorOccurred;

        private Dictionary<EntityBrowse, IEntityCollection> m_CriteriaBrowseLookup;

        private IWorkflowEventService m_WorkflowEventService;
        #endregion

        #region Constants

        /// <summary>
        /// The command apply
        /// </summary>
        public const string CommandApply = "DO";

        /// <summary>
        /// The command button ok
        /// </summary>
        public const string CommandButtonOk = "OK";

        /// <summary>
        /// The command button cancel
        /// </summary>
        public const string CommandButtonCancel = "XX";

        /// <summary>
        /// The command button apply
        /// </summary>
        public const string CommandButtonApply = "AP";

        /// <summary>
        /// The command button focus
        /// </summary>
        public const string CommandButtonFocus = "FF";
        /// <summary>
        /// The command clear
        /// </summary>
        public const string CommandClear = "CLEAR";

        /// <summary>
        /// The command remove
        /// </summary>
        public const string CommandRemove = "REMOVE";

        /// <summary>
        /// The beep error
        /// </summary>
        public const string BeepErrorFile = "beeperror.wav";

        /// <summary>
        /// The beep information
        /// </summary>
        public const string BeepInfoFile = "beepinfo.wav";

        /// <summary>
        /// The beep success
        /// </summary>
        public const string BeepSuccessFile = "beepsuccess.wav";

        /// <summary>
        /// The sound directory
        /// </summary>
        protected const string SoundDirectory = "sound";

        /// <summary>
        /// The resource logical
        /// </summary>
        protected const string ResourceLogical = "smp$resource";

        /// <summary>
        /// The information color
        /// </summary>
        protected readonly Color ColorInfo = Color.CornflowerBlue;

        /// <summary>
        /// The error color
        /// </summary>
        protected readonly Color ColorError = Color.Firebrick;

        /// <summary>
        /// The success color
        /// </summary>
        protected readonly Color ColorSuccess = Color.SeaGreen;

        #endregion

        public UPendingSampleModifyTask()
        {

        }

        protected override void MainFormCreated()
        {
            m_Form = (FormSampleReceipt)MainForm;
            m_UnboundGrid = m_Form.SampleUnboundGrid;
            m_Title = GetFormName();
        }

        protected override void MainFormLoaded()
        {
            if (Context.LaunchMode == GenericLabtableTask.ModifyOption)
            {
                m_Form.Title = m_Title;

                IEntityCollection sampleCollection = EntityManager.CreateEntityCollection<SampleBase>();
                foreach (ScannedSampleBase selectedItem in Context.SelectedItems)
                {
                    entityTemplate = (EntityTemplateInternal)selectedItem.EntityTemplate;

                    //Check that the entityTemplate is the same for all entries selected. Can we handle in context? Does it matter?
                    //if (selectedItem == Context.SelectedItems[0] as PendingSampleBase)
                    //{
                    //    Context.SelectedItems.ActiveItems.Cast<PendingSampleBase>().Any(x => x.EntityTemplate != entityTemplate) ?? throw new SampleManagerError("EntityTemplates are different");
                    //}

                    string serializedJson = selectedItem.ClobToString(PendingSamplePropertyNames.Clob);
                    SampleBase deserializedSample = EntityManager.CreateEntity<SampleBase>();
                    EntityTemplateHelper.DeserializeJSONUsingEntityTemplate(selectedItem, entityTemplate, serializedJson, deserializedSample);

                    sampleCollection.Add(deserializedSample);

                }
            }

            AddPropertyColumns();
            m_CriteriaBrowseLookup = new Dictionary<EntityBrowse, IEntityCollection>();

            //m_Form.ScanBox.EditValueChanged += ScanBox_EditValueChanged;
            //m_Form.SampleUnboundGrid.RowAdded += SampleUnboundGrid_RowAdded;
        }

        /// <summary>
        /// Removes the row.
        /// </summary>
        /// <returns></returns>
        protected virtual bool RemoveRow()
        {
            var gridRow = m_UnboundGrid.FocusedRow;
            //if (!BaseEntity.IsValid(entity)) return false;
            //ShowInfo(m_Form.StringTable.DataRemove, EntityName, entity.Name);
            m_UnboundGrid.RemoveRow(gridRow);
            return true;
        }

        /// <summary>
        /// Removes the row.
        /// </summary>
        /// <returns></returns>
        private void ClearRows()
        {
            m_UnboundGrid.ClearRows();
        }

        /// <summary>
        /// Applies the changes.
        /// </summary>
        private void ApplyChanges()
        {
            try
            {
                if (OnPreSave())
                {
                    EntityManager.Commit();
                    OnPostSave();
                }
            }
            catch (Exception ex)
            {
                //ShowError(ex.Message);
                Logger.Error(ex.Message, ex);
            }
        }

        /// <summary>
		/// Populates the default columns.
		/// </summary>
		/// <param name="grid">The grid.</param>
		/// <param name="row">The row.</param>
		/// <param name="entity">The entity.</param>
		private void PopulateDefaultColumns(UnboundGrid grid, UnboundGridRow row, IEntity entity)
        {
            //if (grid == m_JobPropertiesGrid)
            //{
            //    // Set Job Name

            //    JobHeader jobHeader = (JobHeader)entity;
            //    row[JobNameColumn] = jobHeader.JobName;

            //    if (m_LotDetails != null)
            //    {
            //        jobHeader.LotId = m_LotDetails;
            //        row[LotIdColumn] = jobHeader.LotId;
            //    }

            //    return;
            //}

            //if (grid == m_SamplePropertiesGrid)
            //{
            //TODO - Only need the part here
            //Sample sample = (Sample)entity;

            //if (entityWorkflow.typ)
            //{
            //    // Set Job Name

            //    row[JobNameColumn] = sample.JobName.JobName;
            //}

            //// Set Sample ID

            //row[SampleIdColumn] = sample.IdText;
            //    return;
            //}

            //// This is the test grid

            //Test test = (Test)entity;

            //row[SampleIdColumn] = test.Sample.IdText;
            //row[TestIdColumn] = test.TestCount == 1 ? test.Analysis.VersionedAnalysisName : $"{test.Analysis.VersionedAnalysisName}/{test.TestCount}";
            //row[AssignColumn] = test.Assign;
        }

        #region Specific Type Prompts
        /// <summary>
        /// Setup the grid column
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="templateProperty">The template property.</param>
        /// <param name="row">The row.</param>
        /// <param name="column">The column.</param>
        private void SetupGridColumn(IEntity entity, EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
        {
            //if (entity.EntityType == TestBase.EntityName)
            //{
            //    SetupTestGridColumnInternal((Test)entity, templateProperty, row, column);
            //    SetupTestGridColumn((Test)entity, templateProperty, row, column);
            //    return;
            //}

            //if (entity.EntityType == JobHeaderBase.EntityName)
            //{
            //    SetupJobGridColumn((JobHeader)entity, templateProperty, row, column);
            //    return;
            //}

            if (entity.EntityType == SampleBase.EntityName)
            {
                SetupSampleGridColumnInternal(templateProperty, row, column);
                SetupSampleGridColumn((Sample)entity, templateProperty, row, column);
            }
        }

        /// <summary>
        /// Setup the sample grid column.
        /// </summary>
        /// <param name="templateProperty">The template property.</param>
        /// <param name="row">The row.</param>
        /// <param name="column">The column.</param>
        private static void SetupSampleGridColumnInternal(EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
        {
            // Spreadsheet login does not allow you to change the test schedule during login.

            if (templateProperty.PropertyName == SamplePropertyNames.TestSchedule)
            {
                column.DisableCell(row, DisabledCellDisplayMode.GreyShowContents);
            }
        }

        /// <summary>
		/// Setup the sample grid column.
		/// </summary>
		/// <param name="sample">The sample.</param>
		/// <param name="templateProperty">The template property.</param>
		/// <param name="row">The row.</param>
		/// <param name="column">The column.</param>
		protected virtual void SetupSampleGridColumn(Sample sample, EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
        {
            if (templateProperty.PropertyName == SamplePropertyNames.JobName)
            {
                if (sample.IsSplit)
                    column.DisableCell(row, DisabledCellDisplayMode.GreyShowContents);
                else
                {
                    var columnValue = row.GetValue(column.Name);

                    if (!string.IsNullOrWhiteSpace(columnValue?.ToString()))
                    {
                        column.DisableCell(row, DisabledCellDisplayMode.GreyShowContents);
                    }
                }
            }
        }
        #endregion

        protected override void SetupTask()
        {
            base.SetupTask();
        }

        protected virtual void AddPropertyColumns()
        {
            try
            {
                foreach (EntityTemplateProperty property in entityTemplate.EntityTemplateProperties)
                {
                    // Retrieve or create column

                    UnboundGridColumn gridcolumn = m_UnboundGrid.GetColumnByName(property.PropertyName);

                    if (gridcolumn != null) continue;

                    gridcolumn = m_UnboundGrid.AddColumn(property.PropertyName, property.LocalTitle, "Properties", 100);

                    //if (entityTemplate.TableName == TestBase.EntityName && property.PropertyName == TestPropertyNames.Instrument)
                    //{
                    //    // Instruments must be available and not retired
                    //    IQuery query = EntityManager.CreateQuery(InstrumentBase.EntityName);
                    //    query.AddEquals(InstrumentPropertyNames.Available, true);
                    //    query.AddEquals(InstrumentPropertyNames.Retired, false);
                    //    EntityBrowse instrumentBrowse = BrowseFactory.CreateEntityBrowse(query);
                    //    gridcolumn.SetColumnBrowse(instrumentBrowse);
                    //}
                    //else
                    //{
                    gridcolumn.SetColumnEditorFromObjectModel(entityTemplate.TableName, property.PropertyName);
                    //}

                    if (property.PromptType.IsPhrase(PhraseEntTmpPt.PhraseIdHIDDEN))
                    {
                        gridcolumn.Visible = false;
                    }
                }
            }

            catch (Exception ex)
            {
                throw new SampleManagerError(ex.Message);
            }
        }


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
    }
}
