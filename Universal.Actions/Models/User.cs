using Newtonsoft.Json;

namespace Universal.Actions.Models
{
    public class User
    {

        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }

        //[JsonProperty("upn")]
        //public string UPN { get; set; }
        
        [JsonProperty("approved")]
        public string Approved { get; set; }
    }
}
