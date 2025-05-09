using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Tasks;

namespace Customization.Tasks
{
    [SampleManagerTask(nameof(SampleLoginTask))]
    public class FTISampleLoginTask : SampleLoginTask
    {
        SimpleTreeList _treeList;
        UnboundGrid _testAssignmentGrid;
        private UnboundGrid _samplePropertyGrid;
        private ToolBarButton _addTestButton;

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            _treeList = (MainForm as FormSampleAdmin).TreeListItems;
            _testAssignmentGrid = (MainForm as FormSampleAdmin).TestAssignmentGrid;
            _samplePropertyGrid = (MainForm as FormSampleAdmin).GridSampleProperties;

            _samplePropertyGrid.CellValueChanged += _samplePropertyGrid_CellValueChanged;
            _treeList.NodeAdded += TreeListItems_NodeAdded;
            _testAssignmentGrid.CellValueChanged += TestAssignmentGrid_CellValueChanged;
            _addTestButton = m_Form.ToolBarTree.FindButton("ButtonAddTest");
            _addTestButton.Click += _addTestButton_OnClick;
        }

        private void _samplePropertyGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            UpdateDisplayText2(e);
        }

        private void UpdateDisplayText2(UnboundGridValueChangedEventArgs e)
        {
            //Check if Job
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

            //Check if Sample
            if (e.Row.Tag is Sample sampleNode)
            {
                var node = GetTreeNodeByEntity(sampleNode);
                if (node != null)
                {
                    //Check if Storage Sample
                    if (sampleNode.WorkflowNode.WorkflowId == "08962026-AE43-438F-89CF-30E6F04C2D6D"
                        || sampleNode.WorkflowNode.WorkflowId == "4E9191F5-FE7C-4B09-BE99-E2FAD91B311B")
                    {
                        node.DisplayText = $"Storage (Norm {e.Row.GetValue("FtiTimepoint")} {e.Row.GetValue("FtiTemperature")} {e.Row.GetValue("FtiMediaType")})";
                    }

                    //Check if Test Sample
                    if(sampleNode.WorkflowNode.WorkflowId == "1A5C4AD8-EE48-4FA7-94FC-EF7F44736836" ||
                       sampleNode.WorkflowNode.WorkflowId == "38EC2B8E-A983-4E1C-8794-EA353276447F" ||
                       sampleNode.WorkflowNode.WorkflowId == "5AFC408F-6457-477F-AA08-0A245D866F7C" ||
                       sampleNode.WorkflowNode.WorkflowId == "E84A8E13-066D-46AF-9321-46940584D71B" ||
                       sampleNode.WorkflowNode.WorkflowId == "F915FBAB-DD12-4FFE-9051-64FF338133C0")
                    {
                        node.DisplayText = $"Storage (Norm {e.Row.GetValue("FtiTimepoint")} {e.Row.GetValue("FtiTemperature")} {e.Row.GetValue("FtiMediaType")})";
                    }
                }
            }
        }

        private async void _addTestButton_OnClick(object sender, EventArgs e)
        {
            //Allow 10 seconds for the user to select something
            await Task.Delay(5000);
            UpdateDisplayText(_treeList.FocusedNode);
        }

        private void TestAssignmentGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (e.Column.Tag is object[] data)
            {
                UpdateDisplayText(e.Row.Tag as Sample);
            }
        }

        private void TreeListItems_NodeAdded(object sender, SimpleTreeListNodeEventArgs e)
        {
            UpdateDisplayText(e.Node);
        }

        private void UpdateDisplayText(SimpleTreeListNodeProxy node)
        {
            if (node.Data is Sample sample)
            {
                node.DisplayText = $"Sample ({string.Join(", ", sample.Tests.ActiveItems.Cast<Test>().Where(x => x.Assign).Select(x => x.Analysis.Name))})";
            }
        }

        private void UpdateDisplayText(IEntity entity)
        {
            var node = _treeList.FindNodeByData(entity);
            UpdateDisplayText(node);
        }

        private SimpleTreeListNodeProxy GetTreeNodeByEntity(IEntity entity)
        {
            var node = _treeList.FindNodeByData(entity);
            return node;
        }
    }
}
