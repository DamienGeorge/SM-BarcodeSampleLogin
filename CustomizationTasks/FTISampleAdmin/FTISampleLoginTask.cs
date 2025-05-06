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
        private ToolBarButton _addTestButton;

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            _treeList = (MainForm as FormSampleAdmin).TreeListItems;
            _testAssignmentGrid = (MainForm as FormSampleAdmin).TestAssignmentGrid;

            _treeList.NodeAdded += TreeListItems_NodeAdded;
            _testAssignmentGrid.CellValueChanged += TestAssignmentGrid_CellValueChanged;
            _addTestButton = m_Form.ToolBarTree.FindButton("ButtonAddTest");
            _addTestButton.Click += _addTestButton_OnClick;
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
    }
}
