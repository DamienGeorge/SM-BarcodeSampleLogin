using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Common.Extensions;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server.Workflow.Nodes;
using Thermo.SampleManager.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Customization.Tasks
{
    [SampleManagerTask(nameof(SampleLoginTask))]
    public class FTISampleLoginTask : SampleLoginTask
    {
        SimpleTreeList _treeList;
        UnboundGrid _testAssignmentGrid;
        private UnboundGrid _samplePropertyGrid;
        private UnboundGrid _testPropertyGrid;
        private UnboundGrid _jobPropertyGrid;
        private ToolBarButton _addTestButton;
        private string language;

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            _treeList = (MainForm as FormSampleAdmin).TreeListItems;
            _testAssignmentGrid = (MainForm as FormSampleAdmin).TestAssignmentGrid;
            _jobPropertyGrid = (MainForm as FormSampleAdmin).GridJobProperties;
            _samplePropertyGrid = (MainForm as FormSampleAdmin).GridSampleProperties;
            _testPropertyGrid = (MainForm as FormSampleAdmin).GridTestProperties;

            _jobPropertyGrid.CellValueChanged += _jobPropertyGrid_CellValueChanged;
            _samplePropertyGrid.CellValueChanged += _samplePropertyGrid_CellValueChanged;
            _testPropertyGrid.CellValueChanged += _testPropertyGrid_CellValueChanged;

            _treeList.NodeAdded += TreeListItems_NodeAdded;

            _testAssignmentGrid.CellValueChanged += TestAssignmentGrid_CellValueChanged;
            _testAssignmentGrid.RowAdded += _testAssignmentGrid_RowAdded;
            _testAssignmentGrid.ColumnAdded += _testAssignmentGrid_ColumnAdded;
            language = Library.Environment.GetGlobalUserLanguage();
        }

        private void _jobPropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (e.Row.Tag is JobHeader jobNode)
            {
                var node = _treeList.FindNodeByData(jobNode);

                if (node != null)
                {

                    if (jobNode.WorkflowNode.WorkflowId == "09CB4052-A593-487F-B72E-38932F91C901"
                        || jobNode.WorkflowNode.WorkflowId == "89F8D090-DD59-457C-B7CD-EC37FFB2EE87")
                    {
                        node.DisplayText = $"{e.Row.GetValue("FtiSapNumber")} {e.Row.GetValue(JobHeaderPropertyNames.BrowseDescription)}";
                    }
                }
            }
        }

        //Component list is set or anything else in the test grid is changed
        private void _testPropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            var rows = (sender as UnboundGrid).Rows.Where(x => x.Tag == e.Row.Tag).Distinct();

            UpdateSampleText(rows);
        }

        private void _testAssignmentGrid_ColumnAdded(object sender, UnboundGridColumnEventArgs e)
        {
            var rows = (sender as UnboundGrid).Rows.Distinct();

            UpdateSampleText(rows);
        }

        private void _testAssignmentGrid_RowAdded(object sender, UnboundGridRowAddedEventArgs e)
        {
            var rows = (sender as UnboundGrid).Rows.Distinct();

            UpdateSampleText(rows);
        }

        private void TestAssignmentGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            //use m_innerList
            var rows = (sender as UnboundGrid).Rows.Where(x => x.Tag == e.Row.Tag).Distinct();

            UpdateSampleText(rows);
        }

        /// <summary>
        /// SampleGrid values are only updated when switched. This method checks if the sample in consideration is present in the SampleGrid before processing the sample
        /// </summary>
        /// <param name="rows"></param>
        private void UpdateSampleText(IEnumerable<UnboundGridRow> rows)
        {
            foreach (var row in rows)
            {
                var node = _treeList.FindNodeByData(row.Tag);

                var sampleGridRow = _samplePropertyGrid.GetRowByTag(row.Tag);

                if (sampleGridRow != null)
                {
                    UpdateSampleDisplayTextByRow(sampleGridRow);
                }
                else
                {
                    UpdateSampleDisplayTextByNode(node);
                }
            }
        }

        private void _samplePropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            UpdateSampleDisplayTextByRow(e.Row);
        }

        /// <summary>
        /// Update the Sample Display Text using the data from the SampleGrid Row 
        /// </summary>
        /// <param name="row"></param>
        private void UpdateSampleDisplayTextByRow(UnboundGridRow row)
        {
            if (row != null)
            {
                if (row.Tag is Sample sampleNode)
                {
                    var node = _treeList.FindNodeByData(sampleNode);

                    if (node != null)
                    {
                        var workflowId = sampleNode.WorkflowNode.WorkflowId;

                        //Check if Miscellaneous Sample
                        if (workflowId == "15B20636-1FF2-4511-B4DE-2E93FD0E33B0")
                        {
                            if (language == "EN-GB")
                            {
                                node.DisplayText = "Misc Activities";
                            }
                            else
                            {
                                node.DisplayText = $"Probenübergreifende Aktivitäten";
                            }
                        }

                        //Check if Material Sample
                        if (workflowId == "10C37E39-66F9-424F-B575-3A2060C2F395")
                        {
                            node.DisplayText = $"{row.GetValue(SamplePropertyNames.SampleName).ToString()} ({row.GetValue(SamplePropertyNames.FtiMaterialType)})";
                        }

                        //Check if Statistical/Storage Sample
                        if (workflowId == "08962026-AE43-438F-89CF-30E6F04C2D6D")
                        {
                            if (language == "EN-GB")
                            {
                                node.DisplayText = "Initial Sample";
                            }
                            else
                            {
                                node.DisplayText = $"Anlieferung";
                            }

                            GetTestData(node);
                            //    var text = GetTestData(node);
                            //    node.DisplayText = $"Storage ({text.Trim()} {row.GetValue(SamplePropertyNames.FtiTimepoint)} {row.GetValue(SamplePropertyNames.FtiTemperature)} {row.GetValue(SamplePropertyNames.FtiMediaType)})";
                        }

                        //Check if Statistical/Storage Sample
                        if (workflowId == "4E9191F5-FE7C-4B09-BE99-E2FAD91B311B")
                        {
                            if (language == "EN-GB")
                            {
                                node.DisplayText = "Initial Sample";
                            }
                            else
                            {
                            }

                            node.DisplayText = $"Lagerung in {row.GetValue(SamplePropertyNames.FtiMediaType)} ({row.GetValue(SamplePropertyNames.FtiTimepoint)} {row.GetValue(SamplePropertyNames.FtiTemperature)})";
                            GetTestData(node);
                        }

                        //Check if Test Sample
                        //TODO - decide later
                        if (workflowId == "1A5C4AD8-EE48-4FA7-94FC-EF7F44736836" ||
                           workflowId == "38EC2B8E-A983-4E1C-8794-EA353276447F" ||
                           workflowId == "5AFC408F-6457-477F-AA08-0A245D866F7C" ||
                           workflowId == "E84A8E13-066D-46AF-9321-46940584D71B" ||
                           workflowId == "F915FBAB-DD12-4FFE-9051-64FF338133C0")
                        {
                            node.DisplayText = $"Replikate (Norm {row.GetValue("FtiTimepoint")} {row.GetValue("FtiTemperature")} {row.GetValue("FtiMediaType")})";
                        }

                        //TODO - Remove, only for testing
                        if (workflowId == "D59530B7-C992-4144-8B38-382FA72F1584")
                        {
                            var text = GetTestData(node);
                            node.DisplayText = $"Storage ({text.Trim()} {row.GetValue("SampleName")} , {row.GetValue("Description")})";
                        }

                        if (workflowId == "599868F0-CDD5-4F8D-B590-F3E588B98406")
                        {
                            var text = GetTestData(node);
                            node.DisplayText = $"Sub-Sample ({text} {row.GetValue("SampleName")} {row.GetValue("Description")})";
                        }
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
                if (node.Data is Sample sampleNode)
                {
                    var workflowId = sampleNode.WorkflowNode.WorkflowId;

                    //Check if Material Sample
                    if (workflowId == "10C37E39-66F9-424F-B575-3A2060C2F395")
                    {
                        node.DisplayText = sampleNode.SampleName;
                    }

                    //Check if Storage Sample
                    if (workflowId == "08962026-AE43-438F-89CF-30E6F04C2D6D"
                        || workflowId == "4E9191F5-FE7C-4B09-BE99-E2FAD91B311B")
                    {
                        var text = GetTestData(node);
                        node.DisplayText = $"Storage ({text.Trim()} {sampleNode.FtiTimepoint} {sampleNode.FtiTemperature} {sampleNode.FtiMediaType})";
                    }

                    //Check if Test Sample
                    //TODO - decide later
                    if (workflowId == "1A5C4AD8-EE48-4FA7-94FC-EF7F44736836" ||
                       workflowId == "38EC2B8E-A983-4E1C-8794-EA353276447F" ||
                       workflowId == "5AFC408F-6457-477F-AA08-0A245D866F7C" ||
                       workflowId == "E84A8E13-066D-46AF-9321-46940584D71B" ||
                       workflowId == "F915FBAB-DD12-4FFE-9051-64FF338133C0")
                    {
                        node.DisplayText = $"Replikate (Norm {sampleNode.FtiTimepoint} {sampleNode.FtiTemperature} {sampleNode.FtiMediaType})";
                    }

                    //TODO - Remove, only for testing
                    if (workflowId == "D59530B7-C992-4144-8B38-382FA72F1584")
                    {
                        var text = GetTestData(node);
                        node.DisplayText = $"Storage ({text.Trim()} {sampleNode.SampleName} , {sampleNode.Description})";
                    }

                    if (workflowId == "599868F0-CDD5-4F8D-B590-F3E588B98406")
                    {
                        var text = GetTestData(node);
                        node.DisplayText = $"Storage ({text.Trim()} {sampleNode.SampleName} , {sampleNode.Description})";
                    }

                }
            }
        }

        /// <summary>
        /// Get the tests on the sample
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string GetTestData(SimpleTreeListNodeProxy node)
        {
            string text = String.Empty;

            if (node.Data is Sample sample)
            {
                var tests = sample.Tests?.ActiveItems.Cast<Test>().Where(x => x.Assign).Select(x => x);
                var removedTests = sample.Tests?.ActiveItems.Cast<Test>().Where(x => x.Assign == false).Select(x => x);

                RemoveTestNodes(removedTests);
                text = AddNewTestNodes(node, text, tests);
            }

            return text;
        }

        /// <summary>
        /// Add test node on the tree list. Used only for aesthetic purposes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="text"></param>
        /// <param name="tests"></param>
        /// <returns></returns>
        private string AddNewTestNodes(SimpleTreeListNodeProxy node, string text, IEnumerable<Test> tests)
        {
            if (tests.Count() > 0)
            {
                foreach (var test in tests)
                {
                    var testDisplayText = $"{test.AnalysisTestNumber} {test.ComponentListEntity?.FtiNorm.NormId}";
                    text += testDisplayText;

                    if (_treeList.FindNode(test) == null)
                    {
                        _treeList.AddNode(node, testDisplayText, new IconName("INT_TEST_V"), test);
                    }
                }
            }

            return text;
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
                    var testNode = _treeList.FindNodeByData(test);

                    if (testNode != null)
                    {
                        _treeList.RemoveNode(testNode);
                    }
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
    }
}
