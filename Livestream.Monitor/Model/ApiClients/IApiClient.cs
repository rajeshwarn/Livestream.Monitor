﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExternalAPIs.TwitchTv.Query;
using Livestream.Monitor.Model.Monitoring;

namespace Livestream.Monitor.Model.ApiClients
{
    public interface IApiClient
    {
        string ApiName { get; }

        /// <summary> The base url for the provider (always ending with a forward slash '/')  </summary>
        string BaseUrl { get; }

        bool HasChatSupport { get; }

        bool HasVodViewerSupport { get; }

        bool HasTopStreamsSupport { get; }

        List<string> VodTypes { get; }

        string GetStreamUrl(string channelId);

        /// <summary> Returns null if <see cref="HasChatSupport"/> is false </summary>
        string GetChatUrl(string channelId);

        /// <summary> Updates online streams and returns streams that were <b>not update</b> (offline streams) </summary>
        /// <param name="livestreams">A collection of potentially online streams</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Offline livestreams</returns>
        Task<List<LivestreamModel>> UpdateOnlineStreams(List<LivestreamModel> livestreams, CancellationToken cancellationToken);

        /// <summary> 
        /// Some stream providers such as twitch do not provide stream details for offline streams the same way as online streams. 
        /// </summary>
        Task UpdateOfflineStreams(List<LivestreamModel> livestreams, CancellationToken cancellationToken);

        Task<List<VodDetails>> GetVods(VodQuery vodQuery);

        Task<List<LivestreamModel>> GetTopStreams(TopStreamQuery topStreamQuery);

        /// <summary> Gets a list of known game names from the api with an optional game name filter</summary>
        /// <param name="filterGameName">Optional game name filter</param>
        /// <returns>Collection of known games from the api</returns>
        Task<List<KnownGame>> GetKnownGameNames(string filterGameName);
    }
}