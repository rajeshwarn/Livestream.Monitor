﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchTv.Dto.QueryRoot
{
    public class TopGamesRoot
    {
        [JsonProperty(PropertyName = "_total")]
        public int Total { get; set; }

        public List<Game> Top { get; set; }
    }
}