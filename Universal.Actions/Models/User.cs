using Newtonsoft.Json;

namespace Universal.Actions.Models
{
    public class User
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
