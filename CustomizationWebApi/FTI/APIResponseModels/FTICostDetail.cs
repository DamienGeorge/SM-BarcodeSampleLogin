using System.Runtime.Serialization;

namespace CustomizationWebApi.FTI.APIRequests
{
    [DataContract(Name = "costDetail")]
    public class FTICostDetail
    {
        /// <summary>
        /// Gets or sets the plant ID
        /// </summary>
        /// <value>
        /// Order Number
        /// </value>
        [DataMember(Name = "plantID")]
        public string PlantID { get; set; }
    }
}