using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExternalAPIs.Smashcast.Dto.QueryRoot
{
    public class StreamsRoot
    {
        [JsonProperty("media_type")]
        public string MediaType { get; set; }

        [JsonProperty("livestream")]
        public List<Livestream> Livestreams { get; set; }
    }
}