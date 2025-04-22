using System.Runtime.Serialization;

namespace CustomizationWebApi.FTI.APIRequests
{
    [DataContract(Name = "orderDetail")]
    public class FTIOrderDetail
    {
        /// <summary>
        /// Gets or sets the orderNumber
        /// </summary>
        /// <value>
        /// Order Number
        /// </value>
        [DataMember(Name = "orderNumber")]
        public string OrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>
        /// Action
        /// </value>
        [DataMember(Name = "action")]
        public string Action { get; set; }


        public FTIOrderDetail()
        {

        }
    }
}
