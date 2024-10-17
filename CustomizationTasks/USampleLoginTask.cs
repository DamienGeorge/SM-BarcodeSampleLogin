//using System;
//using System.Collections.Generic;
//using System.Windows.Forms;
//using Thermo.Framework.Core;
//using Thermo.SampleManager.Common.Data;
//using Thermo.SampleManager.Core.Exceptions;
//using Thermo.SampleManager.Internal.ObjectModel;
//using Thermo.SampleManager.Library;
//using Thermo.SampleManager.Library.ClientControls;
//using Thermo.SampleManager.Library.ClientControls.Browse;
//using Thermo.SampleManager.Library.DesignerRuntime;
//using Thermo.SampleManager.Library.EntityDefinition;
//using Thermo.SampleManager.Library.FormDefinition;
//using Thermo.SampleManager.ObjectModel;
//using Thermo.SampleManager.Server;
//using Thermo.SampleManager.Tasks;
//using static System.Windows.Forms.AxHost;
//using Timer = System.Timers.Timer;
//using Environment = System.Environment;
//using System.Drawing;
//using Thermo.SampleManager.Common.Workflow;
//using Thermo.SampleManager.Server.Workflow;
//using Newtonsoft.Json;
////using MethodTimer;

//namespace ISLAND.Tasks
//{
//    [SampleManagerTask("USampleLoginTask")]
//    public class USampleLoginTask : DefaultFormTask
//    {
//        #region Member variables
//        private Workflow entityWorkflow;
//        private EntityTemplateInternal entityTemplate;
//        private FormSampleLogin m_Form;
//        private UnboundGrid m_UnboundGrid;
//        string scannedToField = String.Empty;

//        //For Barcode Scanner
//        private readonly Timer m_TextChangedTimer = new Timer();
//        private TextChangedEventArgs m_TextChangedEventData;
//        private string m_Title;

//        private IQuery m_CriteriaQuery;
//        private bool m_InitialisingCriteria;

//        /// <summary>Indicate that a fatal error occurred - also used as a quick way out</summary>
//        protected bool FatalErrorOccurred;

//        private Dictionary<EntityBrowse, IEntityCollection> m_CriteriaBrowseLookup;

//        private IWorkflowEventService m_WorkflowEventService;
//        #endregion

//        #region Constants

//        /// <summary>
//        /// The command apply
//        /// </summary>
//        public const string CommandApply = "DO";

//        /// <summary>
//        /// The command button ok
//        /// </summary>
//        public const string CommandButtonOk = "OK";

//        /// <summary>
//        /// The command button cancel
//        /// </summary>
//        public const string CommandButtonCancel = "XX";

//        /// <summary>
//        /// The command button apply
//        /// </summary>
//        public const string CommandButtonApply = "AP";

//        /// <summary>
//        /// The command button focus
//        /// </summary>
//        public const string CommandButtonFocus = "FF";
//        /// <summary>
//        /// The command clear
//        /// </summary>
//        public const string CommandClear = "CLEAR";

//        /// <summary>
//        /// The command remove
//        /// </summary>
//        public const string CommandRemove = "REMOVE";

//        /// <summary>
//        /// The beep error
//        /// </summary>
//        public const string BeepErrorFile = "beeperror.wav";

//        /// <summary>
//        /// The beep information
//        /// </summary>
//        public const string BeepInfoFile = "beepinfo.wav";

//        /// <summary>
//        /// The beep success
//        /// </summary>
//        public const string BeepSuccessFile = "beepsuccess.wav";

//        /// <summary>
//        /// The sound directory
//        /// </summary>
//        protected const string SoundDirectory = "sound";

//        /// <summary>
//        /// The resource logical
//        /// </summary>
//        protected const string ResourceLogical = "smp$resource";

//        /// <summary>
//        /// The information color
//        /// </summary>
//        protected readonly Color ColorInfo = Color.CornflowerBlue;

//        /// <summary>
//        /// The error color
//        /// </summary>
//        protected readonly Color ColorError = Color.Firebrick;

//        /// <summary>
//        /// The success color
//        /// </summary>
//        protected readonly Color ColorSuccess = Color.SeaGreen;

//        #endregion

//        public USampleLoginTask()
//        {
//            // Delay processing the scan box

//            m_TextChangedTimer.Elapsed += KeypressedTimerTick;
//            m_TextChangedTimer.Interval = 200;
//        }

//        protected override void MainFormCreated()
//        {
//            m_Form = (FormSampleLogin)MainForm;
//            m_UnboundGrid = m_Form.SampleUnboundGrid;
//            m_Title = GetFormName();
//        }

//        protected override void MainFormLoaded()
//        {
//            var menuparams = Context.MenuItem.Get(MasterMenuPropertyNames.Parameters)?.ToString();

//            if (menuparams is null)
//            {
//                throw new ArgumentNullException("Please Ensure that the following parameters are provided : EntityTemplateId, WorkflowGuid, WorkflowVersion");
//            }

//            var parameters = menuparams.Split(',');

//            var entityTemplateId = String.Empty;
//            var workflowGUID = String.Empty;
//            var workflowVersion = String.Empty;

//            if (parameters.Length > 3)
//            {
//                workflowVersion = parameters[3];
//            }

//            if (parameters.Length > 2)
//            {
//                entityTemplateId = parameters[1];
//                workflowGUID = parameters[2];
//                scannedToField = parameters[0];
//            }
//            else
//            {
//                throw new ArgumentException("Expected at least 3 parameters. Please separate each parameter by a comma");
//            }

//            entityWorkflow = GetWorkflowById(workflowGUID, workflowVersion);

//            if (!(entityWorkflow.WorkflowType.PhraseId == PhraseWflowType.PhraseIdSAMPLE))
//            {
//                throw new ArgumentException($"Workflow {entityWorkflow.NameWithVersion()} is not a {PhraseWflowType.PhraseIdSAMPLE} Workflow!");
//            }

//            entityTemplate = GetEntityTemplateById(entityTemplateId);

//            if (CheckFieldToScanExists(entityTemplate, scannedToField) == false)
//            {
//                throw new ArgumentException($"Could not find {scannedToField} in entityTemplate {entityTemplate.NameWithVersion()}");
//            }

//            AddPropertyColumns();
//            m_CriteriaBrowseLookup = new Dictionary<EntityBrowse, IEntityCollection>();

//            m_Form.ScanBox.EditValueChanged += ScanBox_EditValueChanged;
//            //m_Form.SampleUnboundGrid.RowAdded += SampleUnboundGrid_RowAdded;
//        }

//        private bool CheckFieldToScanExists(EntityTemplateInternal entityTemplate, string scannedToField)
//        {
//            return entityTemplate.EntityTemplateProperties.Contains(scannedToField);
//        }

//        #region Scanning

//        /// <summary>
//        /// Handles the EditValueChanged event of the ScanBox control.
//        /// </summary>
//        /// <param name="sender">The source of the event.</param>
//        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
//        private void ScanBox_EditValueChanged(object sender, TextChangedEventArgs e)
//        {
//            lock (m_TextChangedTimer)
//            {
//                m_TextChangedEventData = e;

//                if (m_TextChangedTimer.Enabled)
//                {
//                    // Don't do it now, it will happen when the timer ticks.
//                }
//                else
//                {
//                    // Do it now, but start the timer, so the next one doesn't.

//                    TextChanged();
//                }

//                m_TextChangedTimer.Stop();
//                m_TextChangedTimer.Start();
//            }
//        }

//        /// <summary>
//        /// Keypresseds the timer tick.
//        /// </summary>
//        /// <param name="sender">The sender.</param>
//        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
//        private void KeypressedTimerTick(object sender, EventArgs e)
//        {
//            lock (m_TextChangedTimer)
//            {
//                m_TextChangedTimer.Stop();
//                TextChanged();
//            }
//        }

//        /// <summary>
//        /// Delayed Text Changed Processing
//        /// </summary>
//        private void TextChanged()
//        {
//            if (m_TextChangedEventData == null) return;
//            var e = m_TextChangedEventData;
//            m_TextChangedEventData = null;

//            m_Form.ScanBox.Focus();

//            // Process the Text Changed Event as normal

//            if (e == null || string.IsNullOrEmpty(e.Text)) return;
//            if (e.Text.EndsWith(Environment.NewLine))
//            {
//                var scanItems = e.Text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
//                m_Form.ScanBox.Text = string.Empty;

//                foreach (var scanText in scanItems)
//                {
//                    if (string.IsNullOrEmpty(scanText)) continue;
//                    if (ProcessScanCommand(scanText)) continue;
//                    ScanValidate(scanText);
//                }
//            }
//        }

//        /// <summary>
//        /// Processes the scan command.
//        /// </summary>
//        /// <param name="scanText">The scan text.</param>
//        /// <returns></returns>
//        private bool ProcessScanCommand(string scanText)
//        {
//            switch (scanText)
//            {
//                case CommandApply:
//                    ApplyChanges();
//                    return true;
//                case CommandClear:
//                    ClearRows();
//                    return true;
//                case CommandRemove:
//                    RemoveRow();
//                    return true;
//                default:
//                    return false;
//            }
//        }

//        /// <summary>
//        /// Removes the row.
//        /// </summary>
//        /// <returns></returns>
//        protected virtual bool RemoveRow()
//        {
//            var gridRow = m_UnboundGrid.FocusedRow;
//            //if (!BaseEntity.IsValid(entity)) return false;
//            //ShowInfo(m_Form.StringTable.DataRemove, EntityName, entity.Name);
//            m_UnboundGrid.RemoveRow(gridRow);
//            return true;
//        }

//        /// <summary>
//        /// Removes the row.
//        /// </summary>
//        /// <returns></returns>
//        private void ClearRows()
//        {
//            m_UnboundGrid.ClearRows();
//        }

//        /// <summary>
//        /// Applies the changes.
//        /// </summary>
//        private void ApplyChanges()
//        {
//            try
//            {
//                if (OnPreSave())
//                {
//                    EntityManager.Commit();
//                    OnPostSave();
//                }
//            }
//            catch (Exception ex)
//            {
//                //ShowError(ex.Message);
//                Logger.Error(ex.Message, ex);
//            }
//        }

//        /// <summary>
//        /// Scans the validate.
//        /// </summary>
//        /// <param name="scanText">The scan text.</param>
//        protected virtual bool ScanValidate(string scanText)
//        {
//            //TODO -What to validate? entry exists already?
//            //TODO - Scan to text can possibly be an entity?

//            //TODO - Catch FatalWorfklow Error
//            IList<IEntity> newEntities = RunWorkflowForEntity(null, entityWorkflow, 1);
//            //Add return property for cleaner code
//            AddEntriesToGrid(scanText, newEntities);
//            AddEntriesToDatabase(newEntities);

//            RefreshGrid();
//            m_Form.ApplyButton.Enabled = true;

//            return true;

//        }

//        //Refreshes the DataGrid
//        private void RefreshGrid()
//        {
//            m_Form.PendingSamplesGrid.ForceRefresh();
//        }

//        /// <summary>
//        /// Commit entries to Pending Sample Table
//        /// </summary>
//        /// <param name="newEntities"></param>
//        /// <exception cref="NotImplementedException"></exception>
//        private void AddEntriesToDatabase(IList<IEntity> newEntities)
//        {
//            var personnel = Library.Environment.CurrentUser;
//            foreach (var entry in newEntities)
//            {
//                PendingSampleBase pendingSample = EntityManager.CreateEntity<PendingSampleBase>();
//                pendingSample.Guid = Guid.NewGuid().ToString();
//                pendingSample.CreatedBy = (PersonnelBase)personnel;
//                var serializedEntity = ToJson(entry as SampleBase);
//                pendingSample.StringToClob(PendingSamplePropertyNames.Clob, serializedEntity);
//                //TODO - Check if issues arise for non string fields
//                pendingSample.Barcode = entry.Get(scannedToField).ToString();

//                EntityManager.Transaction.Add(pendingSample);
//            }

//            EntityManager.Commit();
//        }

//        public string ToJson(IEntity entity)
//        {
//            return JsonConvert.SerializeObject(this, Formatting.Indented, JsonSerializerSettings);
//        }

//        /// <summary>
//        /// Gets the json serializer settings.
//        /// </summary>
//        /// <value>
//        /// The json serializer settings.
//        /// </value>
//        private static JsonSerializerSettings JsonSerializerSettings
//        {
//            get
//            {
//                var settings = new JsonSerializerSettings();
//                settings.NullValueHandling = NullValueHandling.Ignore;
//                settings.Converters.Add(new JsonTimeSpanConverter());
//                settings.Converters.Add(new JsonIntConverter());
//                return settings;
//            }
//        }
//        /// <summary>
//        /// Populate Grid with newly added Entries
//        /// </summary>
//        /// <param name="scanText"></param>
//        /// <param name="newEntities"></param>
//        private void AddEntriesToGrid(string scanText, IList<IEntity> newEntities)
//        {
//            try
//            {
//                m_CriteriaBrowseLookup.Clear();

//                // Suspend communication with client
//                m_UnboundGrid.BeginUpdate();

//                foreach (var entity in newEntities)
//                {
//                    //Set Scanned Data into specified field
//                    entity.Set(scannedToField, scanText);

//                    var newRow = m_UnboundGrid.AddRow();

//                    // Populate default column values

//                    PopulateDefaultColumns(m_UnboundGrid, newRow, entity);

//                    // Set cell values and enable/disable redundant cells based on the entity template

//                    for (int i = m_UnboundGrid.FixedColumns; i < m_UnboundGrid.Columns.Count; i++)
//                    {
//                        UnboundGridColumn column = m_UnboundGrid.Columns[i];

//                        // Try getting the template property

//                        EntityTemplatePropertyInternal templateProperty = entityTemplate.GetProperty(column.Name);

//                        if (templateProperty == null)
//                        {
//                            // Disable this cell

//                            column.DisableCell(newRow, DisabledCellDisplayMode.GreyHideContents);
//                            continue;
//                        }

//                        // Set value

//                        newRow[column] = entity.Get(templateProperty.PropertyName);

//                        // This is an active cell

//                        if (templateProperty.IsMandatory)
//                        {
//                            column.SetCellMandatory(newRow);

//                            var name = entity.Name;
//                        }

//                        if (LocationControl.IsLocation(templateProperty.PropertyName))
//                        {
//                            column.ShowCellButton(newRow, LocationControl.LocationBrowseIcon);
//                        }

//                        if (!string.IsNullOrEmpty(templateProperty.FilterBy))
//                        {
//                            // Setup this column for filtering

//                            // Mark the column that is used for filtering
//                            if (templateProperty.FilterBy.Contains(","))
//                            {
//                                var filterBys = templateProperty.FilterBy.Split(',');
//                                foreach (var filter in filterBys)
//                                {
//                                    var filterItem = filter.Trim();
//                                    UnboundGridColumn filterBySourceColumn = m_UnboundGrid.GetColumnByName(filterItem);
//                                    if (filterBySourceColumn != null)
//                                    {
//                                        filterBySourceColumn.Tag = true;
//                                    }

//                                    // Setup filter

//                                    object filterValue = entity.Get(filterItem);

//                                    if (filterValue != null)
//                                    {
//                                        IEntity filterValueEntity = filterValue as IEntity;
//                                        bool isValid = filterValueEntity == null || BaseEntity.IsValid(filterValueEntity);
//                                        if (isValid)
//                                        {
//                                            SetupFilterBy(templateProperty, newRow, column, filterValue);
//                                        }
//                                    }
//                                }
//                            }
//                            else
//                            {
//                                UnboundGridColumn filterBySourceColumn = m_UnboundGrid.GetColumnByName(templateProperty.FilterBy);
//                                if (filterBySourceColumn != null)
//                                {
//                                    filterBySourceColumn.Tag = true;
//                                }

//                                // Setup filter

//                                object filterValue = entity.Get(templateProperty.FilterBy);

//                                if (filterValue != null)
//                                {
//                                    IEntity filterValueEntity = filterValue as IEntity;
//                                    bool isValid = filterValueEntity == null || BaseEntity.IsValid(filterValueEntity);
//                                    if (isValid)
//                                    {
//                                        SetupFilterBy(templateProperty, newRow, column, filterValue);
//                                    }
//                                }
//                            }
//                        }
//                        else if (!string.IsNullOrEmpty(templateProperty.Criteria))
//                        {
//                            // A criteria has been specified for this column, setup the browse

//                            ICriteriaTaskService criteriaTaskService = (ICriteriaTaskService)Library.GetService(typeof(ICriteriaTaskService));

//                            // Once the query is populated the Query Populated Event is raised. This is because the criteria
//                            // could prompt for VGL values or C# values.
//                            // Prompted Criteria is ignored

//                            string linkedType = EntityType.GetLinkedEntityType(entityTemplate.TableName, templateProperty.PropertyName);
//                            CriteriaSaved criteria = (CriteriaSaved)EntityManager.Select(TableNames.CriteriaSaved, new Identity(linkedType, templateProperty.Criteria));

//                            if (BaseEntity.IsValid(criteria))
//                            {
//                                // Generate a query based on the criteria

//                                criteriaTaskService.QueryPopulated += CriteriaTaskService_QueryPopulated;
//                                m_CriteriaQuery = null;
//                                m_InitialisingCriteria = true;
//                                criteriaTaskService.GetPopulatedCriteriaQuery(criteria);
//                                m_InitialisingCriteria = false;

//                                if (m_CriteriaQuery != null)
//                                {
//                                    // Assign the browse to the column

//                                    IEntityCollection browseEntities = EntityManager.Select(m_CriteriaQuery.TableName, m_CriteriaQuery);
//                                    EntityBrowse criteriaBrowse = BrowseFactory.CreateEntityBrowse(browseEntities);
//                                    column.SetCellBrowse(newRow, criteriaBrowse);
//                                    m_CriteriaBrowseLookup[criteriaBrowse] = browseEntities;

//                                    // Make sure the cell's value is present within the browse

//                                    IEntity defaultValueEntity = entity.GetEntity(templateProperty.PropertyName);

//                                    if (BaseEntity.IsValid(defaultValueEntity) && !browseEntities.Contains(defaultValueEntity) && !defaultValueEntity.IsNew())
//                                    {
//                                        // The default value is not within the specified criteria, null out this cell

//                                        newRow[templateProperty.PropertyName] = null;
//                                    }
//                                }
//                            }
//                        }

//                        if (templateProperty.IsReadOnly || !ValidStatusForModify(entity))
//                        {
//                            // Disable the cell but display it's contents

//                            column.DisableCell(newRow, DisabledCellDisplayMode.GreyShowContents);
//                        }
//                        else if (templateProperty.IsHidden)
//                        {
//                            column.Visible = false;
//                        }

//                        // Do specific column stuff 

//                        SetupGridColumn(entity, templateProperty, newRow, column);
//                    }
//                }
//            }
//            finally
//            {
//                // Do a full client refresh
//                m_UnboundGrid.EndUpdate();
//            }
//        }

//        /// <summary>
//		/// Populates the default columns.
//		/// </summary>
//		/// <param name="grid">The grid.</param>
//		/// <param name="row">The row.</param>
//		/// <param name="entity">The entity.</param>
//		private void PopulateDefaultColumns(UnboundGrid grid, UnboundGridRow row, IEntity entity)
//        {
//            //if (grid == m_JobPropertiesGrid)
//            //{
//            //    // Set Job Name

//            //    JobHeader jobHeader = (JobHeader)entity;
//            //    row[JobNameColumn] = jobHeader.JobName;

//            //    if (m_LotDetails != null)
//            //    {
//            //        jobHeader.LotId = m_LotDetails;
//            //        row[LotIdColumn] = jobHeader.LotId;
//            //    }

//            //    return;
//            //}

//            //if (grid == m_SamplePropertiesGrid)
//            //{
//            //TODO - Only need the part here
//            //Sample sample = (Sample)entity;

//            //if (entityWorkflow.typ)
//            //{
//            //    // Set Job Name

//            //    row[JobNameColumn] = sample.JobName.JobName;
//            //}

//            //// Set Sample ID

//            //row[SampleIdColumn] = sample.IdText;
//            //    return;
//            //}

//            //// This is the test grid

//            //Test test = (Test)entity;

//            //row[SampleIdColumn] = test.Sample.IdText;
//            //row[TestIdColumn] = test.TestCount == 1 ? test.Analysis.VersionedAnalysisName : $"{test.Analysis.VersionedAnalysisName}/{test.TestCount}";
//            //row[AssignColumn] = test.Assign;
//        }
//        #endregion

//        #region Workflow
//        /// <summary>
//		/// Runs the workflow for entity.
//		/// </summary>
//		/// <param name="entity">The entity.</param>
//		/// <param name="selectedWorkflow">The selected workflow.</param>
//		/// <param name="count">The count.</param>
//		/// <returns>List of newly created entities.</returns>
//		private IList<IEntity> RunWorkflowForEntity(IEntity entity, Workflow selectedWorkflow, int count)
//        {
//            var newEntities = new List<IEntity>();

//            for (var j = 0; j < count; j++)
//            {
//                // Run the workflow with a property bag for results - used passed in Parameters if available.

//                var propertyBag = selectedWorkflow.Properties ?? GeneratePropertyBag(entity);

//                // Counters

//                propertyBag.Set("$WORKFLOW_MAX", count);
//                propertyBag.Set("$WORKFLOW_COUNT", j + 1);

//                // Perform

//                PerformWorkflow(selectedWorkflow, propertyBag);

//                // Keep track of the newly created entities.

//                var entities = propertyBag.GetEntities(selectedWorkflow.TableName);

//                newEntities.AddRange(entities);
//            }

//            return newEntities;
//        }

//        /// <summary>
//        /// Generates the property bag for the passed entity.
//        /// </summary>
//        /// <returns></returns>
//        private static IWorkflowPropertyBag GeneratePropertyBag(IEntity entity)
//        {
//            // Generate context for the workflow

//            IWorkflowPropertyBag propertyBag = new WorkflowPropertyBag();

//            if (entity != null)
//            {
//                // Pass in the selected parent
//                propertyBag.Add(entity.EntityType, entity);
//            }

//            return propertyBag;
//        }

//        /// <summary>
//		/// Performs the workflow.
//		/// </summary>
//		/// <param name="workflow">The workflow.</param>
//		/// <param name="propertyBag">The property bag.</param>
//		/// <returns></returns>
//		protected bool PerformWorkflow(Workflow workflow, IWorkflowPropertyBag propertyBag)
//        {
//            if (propertyBag == null)
//            {
//                // Perform the workflow & validate its output

//                propertyBag = workflow.Perform();
//            }
//            else
//            {
//                workflow.Perform(propertyBag);
//            }

//            // Make sure the workflow generated something

//            if (propertyBag.Count == 0)
//            {
//                // Un-supported entity type

//                string message = Library.Message.GetMessage("GeneralMessages", "EmptyWorkflowOutput");
//                Library.Utils.FlashMessage(message, m_Title, MessageButtons.OK, MessageIcon.Error, MessageDefaultButton.Button1);

//                return false;
//            }

//            // Exit if there are errors

//            if (propertyBag.Errors.Count > 0)
//            {
//                Library.Utils.FlashMessage(propertyBag.Errors[0].Message, m_Title, MessageButtons.OK, MessageIcon.Error, MessageDefaultButton.Button1);
//                return false;
//            }

//            return true;
//        }

//        /// <summary>
//		/// Handles the QueryPopulated event of the criteriaTaskService control.
//		/// </summary>
//		/// <param name="sender">The source of the event.</param>
//		/// <param name="e">The <see cref="Thermo.SampleManager.Server.CriteriaTaskQueryPopulatedEventArgs"/> instance containing the event data.</param>
//		private void CriteriaTaskService_QueryPopulated(object sender, CriteriaTaskQueryPopulatedEventArgs e)
//        {
//            if (m_InitialisingCriteria)
//                m_CriteriaQuery = e.PopulatedQuery;
//        }

//        /// <summary>
//		/// Setups the filter by.
//		/// </summary>
//		/// <param name="templateProperty">The template property.</param>
//		/// <param name="row">The new row.</param>
//		/// <param name="column">The column.</param>
//		/// <param name="filterValue">The filter value.</param>
//		private void SetupFilterBy(EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column, object filterValue)
//        {
//            // Setup entity browse filtering

//            IQuery filteredQuery = templateProperty.CreateFilterByQuery(filterValue, row);

//            // Setup the property browse for the collection column to browse collection properties for the table.

//            IEntityBrowse browse = BrowseFactory.CreateEntityOrHierarchyBrowse(filteredQuery.TableName, filteredQuery);

//            column.SetCellEntityBrowse(row, browse);
//        }

//        //TODO - NOt necessary for scanning in new entries, so can probably be removed later.

//        /// <summary>
//        /// Valids the status for modify.
//        /// </summary>
//        /// <param name="entity">The entity.</param>
//        /// <returns></returns>
//        protected virtual bool ValidStatusForModify(IEntity entity)
//        {
//            if (entity is JobHeader)
//            {
//                if (!((JobHeader)entity).JobStatus.IsPhrase(PhraseJobStat.PhraseIdC) &&
//                    !((JobHeader)entity).JobStatus.IsPhrase(PhraseJobStat.PhraseIdV) &&
//                    !entity.IsNew())
//                {
//                    return false;
//                }
//            }

//            if (entity is Sample)
//            {
//                if (!((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdC) &&
//                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdW) &&
//                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdV) &&
//                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdU) &&
//                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdB) &&
//                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdD) &&
//                    !entity.IsNew())
//                {
//                    return false;
//                }
//            }

//            if (entity is Test)
//            {
//                if (!((Test)entity).Status.IsPhrase(PhraseTestStat.PhraseIdC) &&
//                    !((Test)entity).Status.IsPhrase(PhraseTestStat.PhraseIdW) &&
//                    !((Test)entity).Status.IsPhrase(PhraseTestStat.PhraseIdV) &&
//                    !((Test)entity).Status.IsPhrase(PhraseTestStat.PhraseIdU) &&
//                    !((Test)entity).Status.IsPhrase(PhraseTestStat.PhraseIdP) &&
//                    !entity.IsNew())
//                {
//                    return false;
//                }
//            }

//            return true;
//        }
//        #endregion

//        #region Specific Type Prompts
//        /// <summary>
//        /// Setup the grid column
//        /// </summary>
//        /// <param name="entity">The entity.</param>
//        /// <param name="templateProperty">The template property.</param>
//        /// <param name="row">The row.</param>
//        /// <param name="column">The column.</param>
//        private void SetupGridColumn(IEntity entity, EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
//        {
//            //if (entity.EntityType == TestBase.EntityName)
//            //{
//            //    SetupTestGridColumnInternal((Test)entity, templateProperty, row, column);
//            //    SetupTestGridColumn((Test)entity, templateProperty, row, column);
//            //    return;
//            //}

//            //if (entity.EntityType == JobHeaderBase.EntityName)
//            //{
//            //    SetupJobGridColumn((JobHeader)entity, templateProperty, row, column);
//            //    return;
//            //}

//            if (entity.EntityType == SampleBase.EntityName)
//            {
//                SetupSampleGridColumnInternal(templateProperty, row, column);
//                SetupSampleGridColumn((Sample)entity, templateProperty, row, column);
//            }
//        }

//        /// <summary>
//        /// Setup the sample grid column.
//        /// </summary>
//        /// <param name="templateProperty">The template property.</param>
//        /// <param name="row">The row.</param>
//        /// <param name="column">The column.</param>
//        private static void SetupSampleGridColumnInternal(EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
//        {
//            // Spreadsheet login does not allow you to change the test schedule during login.

//            if (templateProperty.PropertyName == SamplePropertyNames.TestSchedule)
//            {
//                column.DisableCell(row, DisabledCellDisplayMode.GreyShowContents);
//            }
//        }

//        /// <summary>
//		/// Setup the sample grid column.
//		/// </summary>
//		/// <param name="sample">The sample.</param>
//		/// <param name="templateProperty">The template property.</param>
//		/// <param name="row">The row.</param>
//		/// <param name="column">The column.</param>
//		protected virtual void SetupSampleGridColumn(Sample sample, EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
//        {
//            if (templateProperty.PropertyName == SamplePropertyNames.JobName)
//            {
//                if (sample.IsSplit)
//                    column.DisableCell(row, DisabledCellDisplayMode.GreyShowContents);
//                else
//                {
//                    var columnValue = row.GetValue(column.Name);

//                    if (!string.IsNullOrWhiteSpace(columnValue?.ToString()))
//                    {
//                        column.DisableCell(row, DisabledCellDisplayMode.GreyShowContents);
//                    }
//                }
//            }
//        }
//        #endregion

//        protected override void SetupTask()
//        {
//            base.SetupTask();
//        }

//        protected virtual void AddPropertyColumns()
//        {
//            try
//            {
//                foreach (EntityTemplateProperty property in entityTemplate.EntityTemplateProperties)
//                {
//                    // Retrieve or create column

//                    UnboundGridColumn gridcolumn = m_UnboundGrid.GetColumnByName(property.PropertyName);

//                    if (gridcolumn != null) continue;

//                    gridcolumn = m_UnboundGrid.AddColumn(property.PropertyName, property.LocalTitle, "Properties", 100);

//                    gridcolumn.SetColumnEditorFromObjectModel(entityTemplate.TableName, property.PropertyName);

//                    if (property.PromptType.IsPhrase(PhraseEntTmpPt.PhraseIdHIDDEN))
//                    {
//                        gridcolumn.Visible = false;
//                    }
//                }
//            }

//            catch (Exception ex)
//            {
//                throw new SampleManagerError(ex.Message);
//            }
//        }


//        private EntityTemplateInternal GetEntityTemplateById(string entityTemplateId)
//        {
//            return EntityManager.SelectLatestVersion<EntityTemplateInternal>(entityTemplateId) ?? throw new NullReferenceException($"Could not find EntityTemplate with id : {entityTemplateId}");
//        }

//        private Workflow GetWorkflowById(string workflowGUID, string workflowVersion)
//        {
//            if (String.IsNullOrEmpty(workflowVersion))
//            {
//                return EntityManager.SelectLatestVersion<Workflow>(new Identity(workflowGUID)) ?? throw new NullReferenceException($"Could not find an active workflow with id : {workflowGUID}");
//            }
//            else
//            {
//                return EntityManager.Select<Workflow>(new Identity(workflowGUID, workflowVersion)) ?? throw new NullReferenceException($"Could not find workflow with id : {workflowGUID} and version : {workflowVersion}");
//            }
//        }
//    }
//}
