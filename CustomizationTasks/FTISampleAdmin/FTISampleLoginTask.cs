using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.ClientControls.Browse;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Tasks;

namespace Customization.Tasks
{
    [SampleManagerTask(nameof(SampleLoginTask))]
    public class FTISampleLoginTask : SampleLoginTask
    {
        FTISampleAdminBaseTask fTISampleAdminBase;
        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();
            fTISampleAdminBase = new FTISampleAdminBaseTask(m_Form, Library, Logger, EntityManager);
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

        protected override void OnPostSave()
        {
            base.OnPostSave();
            fTISampleAdminBase.UpdateTreeList(m_RootNode);
        }
    }
}
