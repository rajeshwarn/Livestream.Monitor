using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchTv.Dto.QueryRoot
{
    public class UserFollows
    {
        public List<Follow> Follows { get; set; }

        [JsonProperty(PropertyName = "_total", NullValueHandling = NullValueHandling.Ignore)]
        public int Total { get; set; }
    }
}