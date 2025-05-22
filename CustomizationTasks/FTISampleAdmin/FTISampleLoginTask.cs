using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Common.Extensions;
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
