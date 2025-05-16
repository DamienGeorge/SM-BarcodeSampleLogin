using System;
using System.Linq;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.FormDefinition;
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

            fTISampleAdminBaseTask = new FTISampleAdminBaseTask(m_Form, Library);

            UpdateAllNodes();
        }

        private void UpdateAllNodes()
        {
            fTISampleAdminBaseTask.UpdateTreeList(m_RootNode);
        }
    }
}
