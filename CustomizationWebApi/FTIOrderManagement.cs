using Thermo.SampleManager.Library;
using CoreWCF.Web;
using CoreWCF.OpenApi.Attributes;
using CustomizationWebApi.FTI.APIRequests;
using CustomizationWebApi.Client;

namespace CustomizationWebApi.FTI
{
    /// <summary>
    /// FTI Web Api Task
    /// </summary>
    /// <seealso cref="SampleManagerWebApiTask" />
    [SampleManagerWebApi("orderDetail")]
    [OpenApiBasePath("/")]
    public class FTIOrderManagement : SampleManagerWebApiTask
    {
        #region Endpoints
        [WebInvoke(UriTemplate = "fti/OrderDetail", Method = "POST")]
        [OpenApiOperation(Description = "Receives data regarding OrderDetails")]
        [OpenApiTag("FTI")]
        public bool FetchOrderDetails(FTIOrderDetail fTIOrderDetail)
        {
            try
            {
                Logger.Info($"Starting FetchOrderDetails for order number: {fTIOrderDetail.OrderNumber}");

                if (string.IsNullOrEmpty(fTIOrderDetail.OrderNumber))
                {
                    Logger.Error("Order number is null or empty");
                    throw new ArgumentException("Order number cannot be null or empty", nameof(fTIOrderDetail));
                }

                ProcessSoapClient(fTIOrderDetail.OrderNumber).Wait();
            
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in FetchOrderDetails: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task ProcessSoapClient(string orderNumber)
        {
            ZLIMS_BAPI_ALM_ORD_GET_DETAILClient client = new();

            BAPI_ALM_ORDER_GET_DETAIL request = new BAPI_ALM_ORDER_GET_DETAIL
            {
                NUMBER = orderNumber,
                RETURN = new List<BAPIRET2>().ToArray()
            };

            Logger.Error($"Sending request to BAPI for order: {orderNumber}");
            var response = await client.BAPI_ALM_ORDER_GET_DETAILAsync(request);

            if (response == null)
            {
                Logger.Error("Received null response from BAPI");
                throw new InvalidOperationException("BAPI returned null response");
            }

            Logger.Error($"Successfully retrieved order details for order: {orderNumber}");
            Logger.Debug($"BAPI Response: {response}");
        }
        #endregion
    }
}