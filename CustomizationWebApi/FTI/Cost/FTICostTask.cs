using Thermo.SampleManager.Library;
using CoreWCF.Web;
using CoreWCF.OpenApi.Attributes;
using CustomizationWebApi.FTI.APIRequests;
using CustomizationWebApi.Client;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Server;
using CustomizationWebApi.Client.Cost;

namespace CustomizationWebApi.FTI
{
    /// <summary>
    /// FTI Web Api Task
    /// </summary>
    /// <seealso cref="SampleManagerWebApiTask" />
    [SampleManagerWebApi("CostDetail")]
    [OpenApiBasePath("/")]
    public class FTICostTask : SampleManagerWebApiTask
    {
        #region Endpoints
        [WebInvoke(UriTemplate = "fti/CostDetail", Method = "POST")]
        [OpenApiOperation(Description = "Receives data regarding CostDetails")]
        [OpenApiTag("FTI")]
        public bool FetchCostDetails(FTICostDetail fTICostDetail)
        {
            try
            {
                Logger.Info($"Starting CostDetails for order number: {fTICostDetail.PlantID}");

                if (string.IsNullOrEmpty(fTICostDetail.PlantID))
                {
                    Logger.Error("Plant ID is null or empty");
                    throw new ArgumentException("Plant ID cannot be null or empty", nameof(fTICostDetail));
                }

                FTICostManagement costHelper = new FTICostManagement(EntityManager, Logger);
                costHelper.ProcessSoapClient(fTICostDetail.PlantID).Wait();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in FetchOrderDetails: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        #endregion
    }
}