using System;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Core.Exceptions;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Tasks;
using System.Drawing;
using Customization.Tasks;
using Customization.Tasks.Helper_Classes;

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

        /// <summary>Indicate that a fatal error occurred - also used as a quick way out</summary>
        protected bool FatalErrorOccurred;
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
                IEntityCollection sampleCollection = EntityManager.CreateEntityCollection<SampleBase>();
                m_Form.Title = m_Title;

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

                ExplorerGridHelper gridHelper = new ExplorerGridHelper(EntityManager, Library, m_Form.SampleUnboundGrid, BrowseFactory);
                gridHelper.PopulateColumns(sampleCollection);
            }
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
