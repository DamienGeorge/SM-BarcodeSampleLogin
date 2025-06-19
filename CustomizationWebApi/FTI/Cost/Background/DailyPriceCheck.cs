using Thermo.SampleManager.Common.CommandLine;
using Thermo.SampleManager.Library;

namespace CustomizationWebApi
{
    /// <summary>
    /// Background task for pulling the SAP cost date into SM
    /// </summary>
    //Todo - test this
    [SampleManagerTask(nameof(PriceCheck))]
    public class PriceCheck : SampleManagerTask, IBackgroundTask
    {
        [CommandLineSwitch("plantID", "Plant to fetch the cost details for")]
        public int PlantID { get; set; }
        public async void Launch()
        {
            FTICostManagement cost = new FTICostManagement(EntityManager, Logger);

            try
            {
                cost.ProcessSoapClient(PlantID.ToString()).Wait();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected override void SetupTask()
        {
            base.SetupTask();

            PlantID = 1029;
            Launch();
        }
    }
}
