using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls.Browse;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Tasks;

namespace Customization.Tasks
{
    [SampleManagerTask(nameof(SampleAdminTask))]
    public class FTISampleAdminTask : SampleAdminTask
    {
        private FTISampleAdminBaseTask fTISampleAdminBaseTask;

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            fTISampleAdminBaseTask = new FTISampleAdminBaseTask(m_Form, Library, Logger, EntityManager);

            UpdateAllNodes();
        }

        private void UpdateAllNodes()
        {
            fTISampleAdminBaseTask.UpdateTreeList(m_RootNode);
        }

        protected override void SetupTestGridColumn(Test test, EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
        {
            base.SetupTestGridColumn(test, templateProperty, row, column);

            SetTestGridColumn(test, templateProperty, row, column);
        }

        private void SetTestGridColumn(Test test, EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column)
        {
            if (templateProperty.PropertyName == TestPropertyNames.FtiAnalysisMethod)
            {
                EntityBrowse methodBrowse = base.BrowseFactory.CreateEntityBrowse(test.Analysis.FtiAnalysisMethods);
                methodBrowse.ReturnProperty = "Name";
                column.SetCellBrowse(row, methodBrowse);
            }
        }
    }
}
