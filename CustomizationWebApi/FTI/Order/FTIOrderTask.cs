using CoreWCF.OpenApi.Attributes;
using CoreWCF.Web;
using CustomizationWebApi.Connected_Services.GetOrderDetail;
using CustomizationWebApi.FTI.APIRequests;
using Newtonsoft.Json;
using Thermo.SampleManager.Library;

namespace CustomizationWebApi.FTI
{
    /// <summary>
    /// FTI Web Api Task
    /// </summary>
    /// <seealso cref="SampleManagerWebApiTask" />
    [SampleManagerWebApi("orderDetail")]
    [OpenApiBasePath("/")]
    public class FTIOrderTask : SampleManagerWebApiTask
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

                ProcessOrder(fTIOrderDetail.OrderNumber).Wait();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in FetchOrderDetails: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        //private async Task ProcessSoapClient(string orderNumber)
        //{
        //    ZLIMS_BAPI_ALM_ORD_GET_DETAILClient client = new();

        //    BAPI_ALM_ORDER_GET_DETAIL request = new BAPI_ALM_ORDER_GET_DETAIL
        //    {
        //        NUMBER = orderNumber,
        //        RETURN = new List<BAPIRET2>().ToArray()
        //    };

        //    Logger.Error($"Sending request to BAPI for order: {orderNumber}");
        //    var response = await client.BAPI_ALM_ORDER_GET_DETAILAsync(request);

        //    if (response == null)
        //    {
        //        Logger.Error("Received null response from BAPI");
        //        throw new InvalidOperationException("BAPI returned null response");
        //    }

        //    Logger.Error($"Successfully retrieved order details for order: {orderNumber}");
        //    Logger.Debug($"BAPI Response: {response}");
        //}

        private async Task ProcessOrder(string OrderNumber)
        {
            GetOrderDetail getOrderDetail = new GetOrderDetail(Library, EntityManager, Logger, OrderNumber);

            try
            {
                string fullURL = $"https://citscpicfdev.it-cpi001-rt.cfapps.eu10.hana.ondemand.com/http/thermofisher/order/{OrderNumber}";

                string jsonString = await getOrderDetail.GetRequestAsync(fullURL);

                Logger.Error(JsonConvert.SerializeObject(jsonString, Formatting.Indented));
                JsonConvert.DeserializeObject<Rootobject>(jsonString);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion
    }
}