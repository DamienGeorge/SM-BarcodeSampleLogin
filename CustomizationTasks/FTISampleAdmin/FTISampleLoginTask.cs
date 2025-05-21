using Thermo.SampleManager.Library;
using Thermo.SampleManager.Tasks;

namespace Customization.Tasks
{
    [SampleManagerTask(nameof(SampleLoginTask))]
    public class FTISampleLoginTask : SampleLoginTask
    {
        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();
            FTISampleAdminBaseTask fTISampleAdminBase = new FTISampleAdminBaseTask(m_Form, Library, Logger, EntityManager);
        }
    }
}
