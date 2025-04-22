namespace CustomizationWebApi.FTI
{
    public class CloudEvent
    {
        public string SpecVersion { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public string Id { get; set; }
        public DateTime Time { get; set; }
        public object Data { get; set; }
    }
} 