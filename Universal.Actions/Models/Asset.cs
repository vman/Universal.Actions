using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universal.Actions.Models
{
    public class Asset
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("approvedBy")]
        public List<User> ApprovedBy { get; set; }
    }
}
