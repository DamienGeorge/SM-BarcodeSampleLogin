using Thermo.SampleManager.Library;
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
    }
}
