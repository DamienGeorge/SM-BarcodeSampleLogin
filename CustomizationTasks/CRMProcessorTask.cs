using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.SampleManager.Common.CommandLine;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.ObjectModel;

namespace Customization.Tasks
{
    public class CRMProcessorTask : SampleManagerTask, IBackgroundTask
    {
        protected override void SetupTask()
        {
            if (Context.SelectedItems.Count == 0)
            {
                return;
            }

            Workflow workflow = 
        }

        public void Launch()
        {
            throw new NotImplementedException();
        }
    }
}
