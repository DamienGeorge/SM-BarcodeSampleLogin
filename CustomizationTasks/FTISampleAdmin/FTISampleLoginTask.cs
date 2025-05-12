using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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


        }

        private void _jobPropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (e.Row.Tag is JobHeader jobNode)
            {
                var node = GetTreeNodeByEntity(jobNode);

                if (node != null)
                {

                    if (jobNode.WorkflowNode.WorkflowId == "09CB4052-A593-487F-B72E-38932F91C901"
                        || jobNode.WorkflowNode.WorkflowId == "89F8D090-DD59-457C-B7CD-EC37FFB2EE87")
                    {
                        node.DisplayText = $"{e.Row.GetValue("FtiSapNumber")} {e.Row.GetValue("JobDescription")})";
                    }
                }
            }
        }

        private void _testPropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            UpdateSampleRow(_samplePropertyGrid.FocusedRow);
        }

        private void _testAssignmentGrid_ColumnAdded(object sender, UnboundGridColumnEventArgs e)
        {
            UpdateSampleRow(_samplePropertyGrid.FocusedRow);
        }

        private void _testAssignmentGrid_RowAdded(object sender, UnboundGridRowAddedEventArgs e)
        {
            UpdateSampleRow(_samplePropertyGrid.FocusedRow);
        }

        private void _samplePropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            UpdateSampleRow(e.Row);
        }

        private void UpdateSampleRow(UnboundGridRow row)
        {
            if (row != null)
            {
                if (row.Tag is Sample sampleNode)
                {
                    var node = GetTreeNodeByEntity(sampleNode);
                    if (node != null)
                    {
                        var workflowId = sampleNode.WorkflowNode.WorkflowId;

                        //Check if Material Sample
                        if (workflowId == "10C37E39-66F9-424F-B575-3A2060C2F395")
                        {
                            node.DisplayText = row.GetValue("SampleName").ToString();
                        }

                        //Check if Storage Sample
                        if (workflowId == "08962026-AE43-438F-89CF-30E6F04C2D6D"
                            || workflowId == "4E9191F5-FE7C-4B09-BE99-E2FAD91B311B")
                        {
                            var text = GetTestData(node);
                            node.DisplayText = $"Storage ({text.Trim()} {row.GetValue("FtiTimepoint")} {row.GetValue("FtiTemperature")} {row.GetValue("FtiMediaType")})";
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

        private string GetTestData(SimpleTreeListNodeProxy node)
        {
            string text = String.Empty;

            if (node.Data is Sample sample)
            {
                var tests = sample.Tests?.ActiveItems.Cast<Test>().Where(x => x.Assign).Select(x => new { x.Analysis.Name, x.ComponentListEntity?.FtiNorm.NormId });

                if (tests.Count() > 0)
                {
                    foreach (var test in tests)
                    {
                        text += $"{test.Name} {test.NormId}";
                    }
                }
            }

            return text;
        }

        private SimpleTreeListNodeProxy GetTreeNodeByEntity(IEntity entity)
        {
            var node = _treeList.FindNodeByData(entity);
            return node;
        }

        private void TestAssignmentGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (e.Column.Tag is object[] data)
            {
                UpdateSampleRow(_samplePropertyGrid.FocusedRow);
            }
        }

        private void TreeListItems_NodeAdded(object sender, SimpleTreeListNodeEventArgs e)
        {
            // GetTestData(e.Node);
        }
    }
}
