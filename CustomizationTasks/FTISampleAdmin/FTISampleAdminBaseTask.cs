using System;
using System.Collections.Generic;
using System.Linq;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;
using Thermo.SampleManager.Server.Workflow.Nodes;

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
        #endregion

        SimpleTreeList _treeList;
        UnboundGrid _testAssignmentGrid;
        private UnboundGrid _samplePropertyGrid;
        private UnboundGrid _testPropertyGrid;
        private UnboundGrid _jobPropertyGrid;
        private string language;

        public StandardLibrary Library { get; }
        public Logger Logger { get; }
        public IEntityManager EntityManager { get; }

        public FTISampleAdminBaseTask(FormSampleAdmin MainForm, StandardLibrary library, Logger logger, IEntityManager entityManager)
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
            _testAssignmentGrid.RowAdded += _testAssignmentGrid_RowAdded;
            _testAssignmentGrid.ColumnAdded += _testAssignmentGrid_ColumnAdded;
            language = ((Personnel)library.Environment.CurrentUser).Language.Identity;
            Library = library;
            Logger = logger;
            EntityManager = entityManager;
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

                if (jobHeader.WorkflowNode.WorkflowId == "09CB4052-A593-487F-B72E-38932F91C901"
                    || jobHeader.WorkflowNode.WorkflowId == "89F8D090-DD59-457C-B7CD-EC37FFB2EE87")
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
                //TODO - To remove
                else
                {
                    node.DisplayText = jobHeader.BrowseDescription;
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
        /// Handles the column added event on the TestAssignmentGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _testAssignmentGrid_ColumnAdded(object sender, UnboundGridColumnEventArgs e)
        {
            var rows = (sender as UnboundGrid).Rows.Distinct();

            UpdateTestDisplayText(rows);
        }


        /// <summary>
        /// Handles the row added event on the TestAssignmentGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _testAssignmentGrid_RowAdded(object sender, UnboundGridRowAddedEventArgs e)
        {
            var rows = (sender as UnboundGrid).Rows.Distinct();

            UpdateTestDisplayText(rows);
        }


        /// <summary>
        /// Handles the cell value changed event on the TestAssignmentGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestAssignmentGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            var rows = (sender as UnboundGrid).Rows.Where(x => x.Tag == e.Row.Tag).Distinct();

            UpdateTestDisplayText(rows);
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
                        var workflowId = sample.WorkflowNode.WorkflowId;

                        //Check if Miscellaneous Sample
                        if (workflowId == "15B20636-1FF2-4511-B4DE-2E93FD0E33B0")
                        {
                            node.DisplayText = language == "EN-GB" ? MiscActivitiesEn : MiscActivitiesGr;
                        }

                        //Check if Material Sample
                        if (workflowId == "10C37E39-66F9-424F-B575-3A2060C2F395")
                        {
                            node.DisplayText = $"{row.GetValue(SamplePropertyNames.SampleName).ToString()} ({row.GetValue(SamplePropertyNames.FtiMaterialType)})";
                        }

                        //Check if Statistical Sample
                        if (workflowId == "08962026-AE43-438F-89CF-30E6F04C2D6D")
                        {
                            node.DisplayText = language == "EN-GB" ? StatisticalSampleEn : StatisticalSampleGr;
                        }

                        //Check if Statistical Storage Sample
                        if (workflowId == "4E9191F5-FE7C-4B09-BE99-E2FAD91B311B")
                        {
                            node.DisplayText = language == "EN-GB" ? StorageStatisticalSampleEn : StorageStatisticalSampleGr;
                            node.DisplayText = $"{node.DisplayText} {row.GetValue(SamplePropertyNames.FtiMediaType)} ({row.GetValue(SamplePropertyNames.FtiTimepoint)}{row.GetValue(SamplePropertyNames.FtiTimeUnit).ToString().Trim()} {row.GetValue(SamplePropertyNames.FtiTemperature)}{row.GetValue(SamplePropertyNames.FtiTempUnit).ToString().Trim()})";
                        }

                        //TODO - Remove, only for testing
                        if (workflowId == "D59530B7-C992-4144-8B38-382FA72F1584")
                        {
                            node.DisplayText = $"Storage ({row.GetValue("SampleName")} , {row.GetValue("Description")})";
                        }

                        if (workflowId == "599868F0-CDD5-4F8D-B590-F3E588B98406")
                        {
                            node.DisplayText = $"Sub-Sample ({row.GetValue("SampleName")} {row.GetValue("Description")})";
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
                    var workflowId = sample.WorkflowNode.WorkflowId;

                    //Check if Miscellaneous Sample
                    if (workflowId == "15B20636-1FF2-4511-B4DE-2E93FD0E33B0")
                    {
                        node.DisplayText = language == "EN-GB" ? MiscActivitiesEn : MiscActivitiesGr;
                    }

                    //Check if Material Sample
                    if (workflowId == "10C37E39-66F9-424F-B575-3A2060C2F395")
                    {
                        node.DisplayText = $"{sample.SampleName} ({sample.FtiMaterialType.PhraseText})";
                    }

                    //Check if Statistical Sample
                    if (workflowId == "08962026-AE43-438F-89CF-30E6F04C2D6D")
                    {
                        node.DisplayText = language == "EN-GB" ? StatisticalSampleEn : StatisticalSampleGr;
                    }

                    //Check if Statistical/Storage Sample
                    if (workflowId == "4E9191F5-FE7C-4B09-BE99-E2FAD91B311B")
                    {
                        node.DisplayText = language == "EN-GB" ? StorageStatisticalSampleEn : StorageStatisticalSampleGr;

                        node.DisplayText = $"{node.DisplayText} {sample.FtiMediaType.ToString()} ({sample.FtiTimepoint.ToString()}{sample.FtiTimeUnit.Trim()} {sample.FtiTemperature.ToString()}{sample.FtiTempUnit.Trim()})";
                    }

                    //Check if Replicate Sample
                    if (sample.SampleType.PhraseId == PhraseSampType.PhraseIdREPLICATE)
                    {
                        node.DisplayText = language == "EN-GB" ? $"Replicate {sample.IdText.Substring(sample.IdText.IndexOf('R'))}" : $"Replikate {sample.IdText.Substring(sample.IdText.IndexOf('R'))}";
                    }

                    //TODO - Remove, only for testing
                    if (workflowId == "D59530B7-C992-4144-8B38-382FA72F1584")
                    {
                        node.DisplayText = $"Storage ({sample.SampleName} , {sample.Description})";
                    }

                    if (workflowId == "599868F0-CDD5-4F8D-B590-F3E588B98406")
                    {
                        node.DisplayText = $"Sub-Sample ({sample.SampleName} , {sample.Description})";
                    }

                    GetTestData(sample, node);
                }
            }
        }


        /// <summary>
        /// Update Display Text on the test
        /// </summary>
        /// <param name="rows"></param>
        private void UpdateTestDisplayText(IEnumerable<UnboundGridRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.Tag is Sample sample)
                {
                    UpdateTestDisplayTextBySample(sample);
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
            var tests = sample.Tests?.ActiveItems.Cast<Test>().Where(x => x.Assign).Select(x => x);
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
                var testNode = _treeList.FindNode(test);

                bool.TryParse(testRow.GetValue(TestPropertyNames.FtiCreateReplicate)?.ToString(), out bool createReplicate);

                var componentListString = testRow.GetValue(TestPropertyNames.ComponentList);

                if (componentListString != null)
                {
                    var componentList = EntityManager.Select<VersionedCLHeader>(new Identity(test.Analysis.Identity, test.Analysis.AnalysisVersion, componentListString));

                    Logger.Error($"Component List {componentList?.CompList}");
                    Logger.Error($"Create Replicate {createReplicate}");

                    var displayText = GetTestDisplayText(test, componentList, createReplicate);

                    if (testNode == null)
                    {
                        Logger.Error($"Did not find Test node");
                        var sampleNode = _treeList.FindNode(test.Sample);
                        //Add test node if it hasn't been created yet
                        _treeList.AddNode(sampleNode, displayText, new IconName("INT_TEST_V"), test);
                    }
                    else
                    {
                        Logger.Error($"Found test node");
                        Logger.Error($"{displayText}");
                        testNode.DisplayText = displayText;
                    }
                }
            }
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
                var displayText = $"{test.AnalysisTestNumber} {componentList?.FtiNorm?.FtiNormName} {geometry.Trim()}";

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
                    //check is test exists in testPropertyGrid, in which case it hasn't been updated on the test yet
                    var testRow = _testPropertyGrid.Rows.Where(x => x.Tag == test).FirstOrDefault();

                    string testDisplayText = string.Empty;

                    if (testRow is null)
                    {
                        testDisplayText = GetTestDisplayText(test, test.ComponentListEntity, test.FtiCreateReplicate);
                    }
                    else
                    {
                        UpdateTestDisplayTextByTestRow(testRow);
                    }

                    var testNode = _treeList.FindNode(test);
                    if (testNode == null)
                    {
                        _treeList.AddNode(node, testDisplayText, new IconName("INT_TEST_V"), test);
                    }
                    else
                    {
                        testNode.DisplayText = testDisplayText;
                    }
                }
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
            if (e.Node.Data is Sample)
            {
                UpdateSampleDisplayTextByNode(e.Node);
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
                //Suppress node addition events
                _treeList.SuppressAddEvents = true;

                if (m_RootNode.DisplayText == "Jobs")
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
                else if (m_RootNode.DisplayText == "Samples")
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

                _treeList.SuppressAddEvents = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.InnerException);
                Logger.Error(ex.Message);
                _treeList.SuppressAddEvents = false;
            }
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