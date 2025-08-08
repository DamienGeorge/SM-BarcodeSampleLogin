using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text.Json.Nodes;
using System.Xml;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;
using Thermo.SampleManager.Server.Workflow;
using Thermo.SampleManager.Server.Workflow.Nodes;
using static System.Net.Mime.MediaTypeNames;
using static Thermo.SampleManager.Server.LabelExportHTML;

namespace Customization.Tasks
{
    public class FTISampleAdminBaseTask
    {
        #region Constants
        private const string MiscActivitiesEn = "Misc Activities";
        private const string MiscActivitiesGr = "Probenübergreifende Aktivitäten";
        private const string StatisticalSampleEn = "Initial Sample";
        private const string StatisticalSampleGr = "Anlieferung";
        private const string StorageStatisticalSampleEn = "Storage in";
        private const string StorageStatisticalSampleGr = "Lagerung in ";
        private const string StatisticalSampleEntityId = "FTI_AVG";
        private const string NoteIconName = "NOTE_EDIT";
        private const string CancelTestIconName = "INT_TEST_X";
        private const string jobActionName = "FTI_PRINT_SAMP_LBLS";
        private const string sampleActionName = "FTI_LBL_REP";

        private readonly int SampleAttachmentMasterMenuNumber = 35224;
        private readonly int JobAttachmentMasterMenuNumber = 35225;
        private readonly int TestAttachmentMasterMenuNumber = 35226;
        private readonly int testCancelMasterMenu = 11019;
        #endregion

        SimpleTreeList _treeList;
        UnboundGrid _testAssignmentGrid;
        private UnboundGrid _samplePropertyGrid;
        private UnboundGrid _testPropertyGrid;
        private UnboundGrid _jobPropertyGrid;
        private string language;

        public FormSampleAdmin MainForm { get; }
        public StandardLibrary Library { get; }
        public Logger Logger { get; }
        public IEntityManager EntityManager { get; }
        public string LaunchMode { get; }

        public bool isStartup = false;
        private IEntity currentEntity;
        private ContextMenuItem reportMenu;
        private ContextMenuItem labelMenu;
        private ContextMenuItem cancelTestMenu;
        private readonly ICollection<ReportTemplateMenuInfo> jobReportTemplates;

        public FTISampleAdminBaseTask(FormSampleAdmin MainForm, StandardLibrary library, Logger logger, IEntityManager entityManager, string launchMode)
        {
            _treeList = MainForm.TreeListItems;
            _testAssignmentGrid = MainForm.TestAssignmentGrid;
            _jobPropertyGrid = MainForm.GridJobProperties;
            _samplePropertyGrid = MainForm.GridSampleProperties;
            _testPropertyGrid = MainForm.GridTestProperties;
            _jobPropertyGrid.CellValueChanged += _jobPropertyGrid_CellValueChanged;
            _samplePropertyGrid.CellValueChanged += _samplePropertyGrid_CellValueChanged;
            _testPropertyGrid.CellValueChanged += _testPropertyGrid_CellValueChanged;

            _treeList.NodeAdded += TreeListItems_NodeAdded;

            _testAssignmentGrid.CellValueChanged += TestAssignmentGrid_CellValueChanged;
            //_testAssignmentGrid.RowAdded += _testAssignmentGrid_RowAdded;
            //_testAssignmentGrid.ColumnAdded += _testAssignmentGrid_ColumnAdded;
            language = ((Personnel)library.Environment.CurrentUser).Language.Identity;
            this.MainForm = MainForm;
            Library = library;
            Logger = logger;
            EntityManager = entityManager;
            LaunchMode = launchMode;

            #region Custom RMBs
            var attachmentMenu = _treeList.ContextMenu.AddItem("Edit Attachment(s)", NoteIconName);
            attachmentMenu.ItemClicked += AttachmentMenu_ItemClicked;

            _treeList.ContextMenu.BeforePopup += ContextMenu_BeforePopup;

            reportMenu = _treeList.ContextMenu.AddItem("Reports", "NOTE_EDIT", true);

            jobReportTemplates = GetReportTemplates(TableNames.JobHeader);

            foreach (var reportTemplate in jobReportTemplates)
            {
                var reportHandler = reportMenu.CustomItems.Add(reportTemplate.Name, null);
                reportHandler.ItemClicked += ReportMenu_ItemClicked;
            }

            reportMenu.BeginGroup = false;

            labelMenu = _treeList.ContextMenu.AddItem("Print Sample Label(s)", null);
            labelMenu.ItemClicked += LabelMenu_ItemClicked;

            cancelTestMenu = _treeList.ContextMenu.AddItem("Cancel Test", CancelTestIconName);
            cancelTestMenu.ItemClicked += CancelTestMenu_ItemClicked;
            #endregion
        }

        /// <summary>
        /// Entity is only fetched during before popup. So set entity here
        /// Also set any visibility on the menu for different Context Menu items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenu_BeforePopup(object sender, ContextMenuBeforePopupEventArgs e)
        {
            var entity = e.Entity;

            if (entity is not null)
            {
                currentEntity = entity;

                if (currentEntity is JobHeader job)
                {
                    reportMenu.Visible = true;
                    foreach (ContextMenuItem entry in reportMenu.CustomItems)
                    {
                        entry.Visible = true;
                    }
                }
                else
                {
                    reportMenu.Visible = false;
                    foreach (ContextMenuItem entry in reportMenu.CustomItems)
                    {
                        entry.Visible = false;
                    }
                }

                if (currentEntity is Test test)
                {
                    labelMenu.Visible = false;
                    cancelTestMenu.Visible = true;
                }
                else
                {
                    labelMenu.Visible = true;
                    cancelTestMenu.Visible = false;
                }
            }
        }

        /// <summary>
        /// Handles the Cancel Test Click Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelTestMenu_ItemClicked(object sender, ContextMenuItemEventArgs e)
        {
            if (currentEntity is not null && currentEntity is Test test)
            {
                try
                {
                    currentEntity.LockRelease();
                    MainForm.SetBusy();

                    var result = Library.Task.CreateTaskAndWait(testCancelMasterMenu, null, new EntityCollection() { test });

                    MainForm.ClearBusy();
                    currentEntity.Lock();
                    RemoveTest(test);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MainForm.ForceClearBusy();
                    currentEntity.Lock();
                }
            }
        }

        /// <summary>
        /// Update test icon
        /// </summary>
        /// <param name="test"></param>
        private void RemoveTest(Test test)
        {
            var testNode = _treeList.FindNode(test);

            _treeList.RemoveNode(testNode);
        }

        /// <summary>
        /// Handles the Label Menu Item Click Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LabelMenu_ItemClicked(object sender, ContextMenuItemEventArgs e)
        {
            if (currentEntity is not null)
            {
                var workflowBag = new WorkflowPropertyBag();
                try
                {
                    WorkflowActionType actionType;
                    if (currentEntity is Sample sample)
                    {
                        actionType = EntityManager.Select<WorkflowActionType>(new Identity(currentEntity.EntityType, sampleActionName));
                    }
                    else
                    {
                        actionType = EntityManager.Select<WorkflowActionType>(new Identity(currentEntity.EntityType, jobActionName));
                    }

                    Library.Utils.SetStatusBar("Generating Label");
                    currentEntity.PerformAction(actionType.Identity, workflowBag);

                    if (workflowBag.HasErrors)
                    {
                        foreach (var entry in workflowBag.Errors)
                        {
                            Logger.Error(entry);
                        }
                        Library.Utils.SetStatusBar("Errors faced during Label Generation. Check Log for errors");
                    }
                    Library.Utils.SetStatusBar("Label Generated");
                }
                catch (Exception ex)
                {
                    Library.Utils.SetStatusBar("Errors faced during Label Generation. Check Log for errors");
                    Logger.Error(ex);
                }
            }
        }

        /// <summary>
        /// Handles the Report Menu Item Click Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportMenu_ItemClicked(object sender, ContextMenuItemEventArgs e)
        {
            if (currentEntity is JobHeader job)
            {
                try
                {
                    ReportTemplateMenuInfo selectedReport = jobReportTemplates.Where(x => x.Name == e.Item.Caption).FirstOrDefault();

                    Library.Utils.SetStatusBar("Generating Report");
                    Library.Reporting.PreviewReport(selectedReport.Identity, currentEntity, new ReportOptions(ReportOutput.Preview));
                    Library.Utils.SetStatusBar("Report Generated");
                }
                catch (Exception ex)
                {
                    Library.Utils.SetStatusBar("Errors faced during Report Generation. Check Log for errors");
                    Logger.Error(ex);
                }
            }
        }

        /// <summary>
        /// Get Matching Report Templates for a table
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private ICollection<ReportTemplateMenuInfo> GetReportTemplates(string tableName)
        {
            List<ReportTemplateMenuInfo> reportTemplates = new List<ReportTemplateMenuInfo>();
            IQuery query = EntityManager.CreateQuery("REPORT_TEMPLATE");
            query.AddEquals("DATA_ENTITY_DEFINITION", tableName);
            query.AddEquals("REMOVEFLAG", false);
            query.AddEquals("PUBLISH", true);
            query.AddEquals("ACTIVE", true);
            query.AddEquals("APPROVAL_STATUS", "A");

            foreach (IEntity entity in EntityManager.Select("REPORT_TEMPLATE", query, true))
            {
                reportTemplates.Add(new ReportTemplateMenuInfo()
                {
                    Identity = entity.GetString("IDENTITY"),
                    Name = entity.GetString("NAME"),
                    Version = entity.GetString("VERSION"),
                    Icon = ""
                });
            }
            return reportTemplates;
        }

        /// <summary>
        /// Defines what should happen after the edit attachments button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttachmentMenu_ItemClicked(object sender, ContextMenuItemEventArgs e)
        {
            if (currentEntity is not null)
            {
                try
                {
                    int masterMenuToRun = 0;

                    currentEntity.LockRelease();
                    MainForm.SetBusy();

                    if (currentEntity is JobHeader job)
                    {
                        masterMenuToRun = JobAttachmentMasterMenuNumber;
                    }
                    else if (currentEntity is Sample sample)
                    {
                        masterMenuToRun = SampleAttachmentMasterMenuNumber;
                    }
                    else if (currentEntity is Test test)
                    {
                        masterMenuToRun = TestAttachmentMasterMenuNumber;
                    }

                    var result = Library.Task.CreateTaskAndWait(masterMenuToRun, null, new EntityCollection() { currentEntity });

                    currentEntity.Lock();
                    MainForm.ClearBusy();
                }
                catch (Exception ex)
                {
                    MainForm.ForceClearBusy();
                    Logger.Error(ex);
                }

            }
        }

        /// <summary>
        /// Handles the Cell Value changed event on the JobPropertyGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _jobPropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (e.Row.Tag is JobHeader jobNode)
            {
                var node = _treeList.FindNodeByData(jobNode);
                UpdateJobDisplayText(jobNode, node, e);
            }
        }

        /// <summary>
        /// Updates the job Display Text
        /// </summary>
        /// <param name="jobHeader"></param>
        /// <param name="node"></param>
        /// <param name="e"></param>
        private static void UpdateJobDisplayText(JobHeader jobHeader, SimpleTreeListNodeProxy node, UnboundGridValueChangedEventArgs e = null)
        {
            if (node != null)
            {
                if (e is not null)
                {
                    node.DisplayText = $"{e.Row.GetValue(JobHeaderPropertyNames.FtiSapNumber)} {e.Row.GetValue(JobHeaderPropertyNames.BrowseDescription)}";
                }
                else
                {
                    node.DisplayText = $"{jobHeader.FtiSapNumber} {jobHeader.BrowseDescription}";
                }
            }
        }

        /// <summary>
        /// Handles the Cell Value changed event on the SamplePropertyGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _samplePropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            UpdateSampleDisplayTextByRow(e.Row);
        }

        //Component list is set or anything else in the test grid is changed
        private void _testPropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            //The value here contains the Test entity unlike the others which contain the sample entity
            UpdateTestDisplayTextByTestRow(e.Row);
        }

        /// <summary>
        /// Handles the cell value changed event on the TestAssignmentGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestAssignmentGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (e.Row.Tag is Sample sample)
            {
                try
                {
                    var columnTag = (e.Column.Tag as object[]);
                    var analysis = columnTag[0];
                    int.TryParse(columnTag[1].ToString(), out int analysisNumber);

                    var test = sample.Tests.ActiveItems.Cast<Test>().Where(x => analysisNumber == 1 ? x.Analysis == analysis : x.AnalysisTestNumber == e.Column.Caption)
                        .FirstOrDefault();

                    //If current test assignment sample is in focus, the test property grid will likely be loaded.
                    if (test.Assign)
                    {
                        var displayText = string.Empty;

                        var unboundGridRow = _testPropertyGrid.Rows?.Where(x => x.Tag == test).FirstOrDefault();

                        if (_treeList.FocusedNode.Data == sample && unboundGridRow != null)
                        {
                            displayText = GetTestDisplayText(unboundGridRow, test);
                        }
                        else
                        {
                            displayText = GetTestDisplayText(test, test.ComponentListEntity, test.FtiCreateReplicate);
                        }
                        _treeList.AddNode(_treeList.FindNodeByData(sample), displayText, GetTestIcon(test), test);

                    }
                    else
                    {
                        RemoveTestNode(test);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }


        /// <summary>
        /// Update the Sample Display Text using the data from the SampleGrid Row 
        /// </summary>
        /// <param name="row"></param>
        private void UpdateSampleDisplayTextByRow(UnboundGridRow row)
        {
            if (row != null)
            {
                if (row.Tag is Sample sample)
                {
                    var node = _treeList.FindNodeByData(sample);

                    if (node != null)
                    {

                        //Check if Miscellaneous Sample
                        if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdMISC_SUMM)
                        {
                            node.DisplayText = language == "EN-GB" ? MiscActivitiesEn : MiscActivitiesGr;
                        }

                        //Check if Material Sample
                        if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdMATERIAL)
                        {
                            var phraseText = string.Empty;

                            try
                            {
                                phraseText = (EntityManager.SelectPhrase(PhraseFtiMatype.Identity, row.GetValue(SamplePropertyNames.FtiMaterialType).ToString()) as Phrase).PhraseText;
                            }
                            catch
                            {
                                //do nothing. lazy null check 
                            }

                            node.DisplayText = $"{row.GetValue(SamplePropertyNames.SampleName).ToString()} ({phraseText})";
                        }

                        //Check if Statistical Sample
                        if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdAVERAGE)
                        {
                            var statisticalSampleTemplate = EntityManager.SelectLatestVersion<EntityTemplate>(StatisticalSampleEntityId);
                            if (sample.EntityTemplate == statisticalSampleTemplate)
                            {
                                node.DisplayText = language == "EN-GB" ? StatisticalSampleEn : StatisticalSampleGr;
                            }
                            //Check if Statistical Storage Sample
                            else
                            {
                                node.DisplayText = language == "EN-GB" ? StorageStatisticalSampleEn : StorageStatisticalSampleGr;
                                node.DisplayText = $"{node.DisplayText} {row.GetValue(SamplePropertyNames.FtiMediaType)} ({row.GetValue(SamplePropertyNames.FtiTimepoint)}{row.GetValue(SamplePropertyNames.FtiTimeUnit).ToString().Trim()} {row.GetValue(SamplePropertyNames.FtiTemperature)}{row.GetValue(SamplePropertyNames.FtiTempUnit).ToString().Trim()})";
                            }
                        }

                        GetTestData(sample, node);
                    }
                }
            }
        }

        /// <summary>
        /// Update the Sample Display Text using the data from the node
        /// </summary>
        /// <param name="node"></param>
        private void UpdateSampleDisplayTextByNode(SimpleTreeListNodeProxy node)
        {
            if (node != null)
            {
                if (node.Data is Sample sample)
                {
                    //Check if Miscellaneous Sample
                    if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdMISC_SUMM)
                    {
                        node.DisplayText = language == "EN-GB" ? MiscActivitiesEn : MiscActivitiesGr;
                    }

                    //Check if Material Sample
                    if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdMATERIAL)
                    {
                        node.DisplayText = $"{sample.SampleName} ({sample.FtiMaterialType.PhraseText})";
                    }

                    //Check if Statistical Sample
                    if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdAVERAGE)
                    {
                        var statisticalSampleTemplate = EntityManager.SelectLatestVersion<EntityTemplate>(StatisticalSampleEntityId);
                        if (sample.EntityTemplate == statisticalSampleTemplate)
                        {
                            node.DisplayText = language == "EN-GB" ? StatisticalSampleEn : StatisticalSampleGr;
                        }
                        else //Check if Statistical/Storage Sample
                        {
                            node.DisplayText = language == "EN-GB" ? StorageStatisticalSampleEn : StorageStatisticalSampleGr;

                            node.DisplayText = $"{node.DisplayText} {sample.FtiMediaType.ToString()} ({sample.FtiTimepoint.ToString()}{sample.FtiTimeUnit.Trim()} {sample.FtiTemperature.ToString()}{sample.FtiTempUnit.Trim()})";
                        }
                    }

                    //Check if Replicate Sample
                    if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdREPLICATE)
                    {
                        string sampleString = sample.IdText.Substring(sample.IdText.IndexOf('R'));
                        node.DisplayText = language == "EN-GB" ? $"Replicate {sampleString}" : $"Replikate {sampleString}";
                    }

                    GetTestData(sample, node);
                }
            }
        }

        /// <summary>
        /// Get the tests on the sample
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        private void GetTestData(Sample sample, SimpleTreeListNodeProxy node)
        {
            var tests = sample.Tests?.ActiveItems.Cast<Test>()
                .Where(x => x.Assign && (x.IsNew() || x.Status.PhraseId is not PhraseTestStat.PhraseIdX)) //Exclude Cancelled Tests
                .Select(x => x);
            var removedTests = sample.Tests?.ActiveItems.Cast<Test>().Where(x => x.Assign == false).Select(x => x);

            RemoveTestNodes(removedTests);
            AddNewTestNodes(node, tests);
        }

        private void UpdateTestDisplayTextByTestRow(UnboundGridRow testRow)
        {
            bool isAssigned = false;
            bool.TryParse(testRow.GetValue("Assign")?.ToString(), out isAssigned);

            if (testRow.Tag is Test test)
            {
                if (isAssigned)
                {
                    var testNode = _treeList.FindNode(test);
                    var displayText = GetTestDisplayText(testRow, test);

                    if (testNode == null)
                    {
                        Logger.Error($"Did not find Test node");
                        var sampleNode = _treeList.FindNode(test.Sample);
                        //Add test node if it hasn't been created yet
                        _treeList.AddNode(sampleNode, displayText, GetTestIcon(test), test);
                    }
                    else
                    {
                        Logger.Error($"Found test node");
                        Logger.Error($"{displayText}");
                        testNode.DisplayText = displayText;
                    }
                }
                else
                {  //If a test is unassigned, then it needs to be removed
                    RemoveTestNode(test);
                }
            }
        }

        private string GetTestDisplayText(UnboundGridRow testRow, Test test)
        {
            bool.TryParse(testRow.GetValue(TestPropertyNames.FtiCreateReplicate)?.ToString(), out bool createReplicate);

            var componentListString = testRow.GetValue(TestPropertyNames.ComponentList);

            if (componentListString != null)
            {
                var componentList = EntityManager.Select<VersionedCLHeader>(new Identity(test.Analysis.Identity, test.Analysis.AnalysisVersion, componentListString));

                Logger.Error($"Component List {componentList?.CompList}");
                Logger.Error($"Create Replicate {createReplicate}");

                return GetTestDisplayText(test, componentList, createReplicate);
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the Test node display text based on user selections
        /// </summary>
        /// <param name="test"></param>
        /// <param name="componentList"></param>
        /// <param name="ftiCreateReplicate"></param>
        /// <returns></returns>
        private string GetTestDisplayText(Test test, VersionedCLHeaderBase componentList, bool ftiCreateReplicate)
        {
            try
            {
                var geometry = ftiCreateReplicate ? componentList?.FtiSampleForm?.PhraseText : string.Empty;
                var displayText = $"{test.AnalysisTestNumber} {componentList?.FtiNorm?.FtiNormName} {geometry?.Trim()}";

                Logger.Error(displayText);
                return displayText;
            }
            catch (Exception ex)
            {
                Logger.Error($"Caught Exception : {ex}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Updates the sample and test nodes on a sample
        /// </summary>
        /// <param name="row"></param>
        private void UpdateTestDisplayTextBySample(Sample sample)
        {
            var node = _treeList.FindNodeByData(sample);
            GetTestData(sample, node);
        }

        /// <summary>
        /// Remove test node on the sample. Used only for aesthetic purposes 
        /// </summary>
        /// <param name="removedTests"></param>
        private void RemoveTestNode(Test removedTest)
        {
            var testNode = _treeList.FindNodeByData(removedTest);

            if (testNode != null)
            {
                _treeList.RemoveNode(testNode);
            }
        }

        /// <summary>
        /// Add test node on the tree list. Used only for aesthetic purposes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="text"></param>
        /// <param name="tests"></param>
        /// <returns></returns>
        private void AddNewTestNodes(SimpleTreeListNodeProxy node, IEnumerable<Test> tests)
        {
            if (tests.Count() > 0)
            {
                foreach (var test in tests)
                {
                    //check if test exists in testPropertyGrid, in which case it hasn't been updated on the test yet if it was just created
                    var testRow = _testPropertyGrid.Rows.Where(x => x.Tag == test).FirstOrDefault();

                    string testDisplayText = string.Empty;

                    //When creating new sample - test created - not showing in navigation, will show if test property grid is upddated
                    //Will not show if updating assignment grid
                    //Test is removed but not added correctly

                    if (test.IsNew() == false && (LaunchMode == "MODIFY" || LaunchMode == "DISPLAY") && isStartup == false)
                    {
                        testDisplayText = GetTestDisplayText(test, test.ComponentListEntity, test.FtiCreateReplicate);
                    }
                    else if (testRow is null || isStartup)
                    {
                        testDisplayText = GetTestDisplayText(test, test.ComponentListEntity, test.FtiCreateReplicate);
                    }
                    else
                    {
                        testDisplayText = GetTestDisplayText(testRow, test);
                    }

                    var testNode = _treeList.FindNode(test);
                    if (testNode == null)
                    {
                        _treeList.AddNode(node, testDisplayText, GetTestIcon(test), test);
                    }
                    else
                    {
                        testNode.DisplayText = testDisplayText;
                    }
                }
            }
        }
        private IconName GetTestIcon(Test test)
        {
            if (test.Status is not null && test.Status.Icon is not null)
            {
                return new IconName(test.Status.Icon?.Identity);
            }
            else
            {
                PhraseBase phrase = EntityManager.SelectPhrase(PhraseTestStat.Identity, PhraseTestStat.PhraseIdV) as PhraseBase;
                return new IconName(phrase?.Icon?.Identity);
            }
        }

        /// <summary>
        /// Remove test nodes that were unassigned on the sample. Used only for aesthetic purposes 
        /// </summary>
        /// <param name="removedTests"></param>
        private void RemoveTestNodes(IEnumerable<Test> removedTests)
        {
            if (removedTests.Count() > 0)
            {
                foreach (var test in removedTests)
                {
                    RemoveTestNode(test);
                }
            }
        }
        /// <summary>
        /// Event Handler for Tree List Node addition
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeListItems_NodeAdded(object sender, SimpleTreeListNodeEventArgs e)
        {
            if (e.Node.Data is Sample sample)
            {
                //TODO - If test is on selected sample. it will not render.
                var gridSample = _samplePropertyGrid.Rows.Where(x => x.Tag == sample).FirstOrDefault();

                if (gridSample is not null)
                {
                    UpdateSampleDisplayTextByRow(gridSample);
                }
                else
                {
                    UpdateSampleDisplayTextByNode(e.Node);
                }
            }

            if (e.Node.Data is JobHeader job)
            {
                var node = _treeList.FindNodeByData(job);
                UpdateJobDisplayText(job, node);
            }
        }

        /// <summary>
        /// Updates the display text for all the tree list entries
        /// </summary>
        /// <param name="m_RootNode"></param>
        public void UpdateTreeList(SimpleTreeListNodeProxy m_RootNode)
        {
            try
            {
                TurnOnStartupConfig();

                if (m_RootNode.FirstNode?.Data is JobHeader)
                {
                    foreach (var node in m_RootNode.Nodes)
                    {
                        if (node.Data is JobHeader jobHeader)
                        {
                            UpdateJobDisplayText(jobHeader, node);

                            UpdateSampleDisplayTextByCollection(jobHeader.RootSamples);
                        }

                    }
                }
                else if (m_RootNode.FirstNode?.Data is Sample)
                {
                    foreach (var node in m_RootNode.Nodes)
                    {
                        if (node.Data is Sample sample)
                        {
                            var sampleCollection = EntityManager.CreateEntityCollection<Sample>();
                            sampleCollection.Add(sample);
                            UpdateSampleDisplayTextByCollection(sampleCollection);
                        }
                    }
                }

                TurnOffStartupConfig();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.InnerException);
                Logger.Error(ex.Message);
                _treeList.SuppressAddEvents = false;
            }
        }

        private void TurnOnStartupConfig()
        {
            //Suppress node addition events
            _treeList.SuppressAddEvents = true;
            isStartup = true;
        }

        private void TurnOffStartupConfig()
        {
            _treeList.SuppressAddEvents = false;
            isStartup = false;
        }

        private void UpdateSampleDisplayTextByCollection(IEntityCollection samples)
        {
            var sampleCollection = new List<Sample>();
            IterateSampleList(samples, ref sampleCollection);

            foreach (var sample in sampleCollection)
            {
                var node = _treeList.FindNode(sample);
                UpdateSampleDisplayTextByNode(node);
            }
        }

        private void IterateSampleList(IEntityCollection samples, ref List<Sample> sampleCollection)
        {
            foreach (Sample sample in samples)
            {
                sampleCollection.Add(sample);

                if (sample.ChildSamples.Count > 0)
                {
                    IterateSampleList(sample.ChildSamples, ref sampleCollection);
                }
            }
        }
    }
}