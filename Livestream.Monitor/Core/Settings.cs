﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Caliburn.Micro;
using Livestream.Monitor.Core.UI;
using Livestream.Monitor.Model;
using Livestream.Monitor.Model.ApiClients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Livestream.Monitor.Core
{
    public class Settings : PropertyChangedBase
    {
        public const string DEFAULT_CHROME_FULL_PATH = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        public const string DEFAULT_LIVESTREAMER_FULL_PATH = @"C:\Program Files (x86)\Livestreamer\livestreamer.exe";
        public const int DEFAULT_MINIMUM_EVENT_VIEWERS = 30000;

        private MetroThemeBaseColour? metroThemeBaseColour;
        private MetroThemeAccentColour? metroThemeAccentColour;
        private StreamQuality defaultStreamQuality;
        private string livestreamerFullPath;
        private string chromeFullPath;
        private int minimumEventViewers = DEFAULT_MINIMUM_EVENT_VIEWERS;
        private bool disableNotifications, passthroughClientId;
        private bool hideStreamOutputMessageBoxOnLoad;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public MetroThemeBaseColour? MetroThemeBaseColour
        {
            get { return metroThemeBaseColour; }
            set
            {
                if (value == metroThemeBaseColour) return;
                metroThemeBaseColour = value;
                NotifyOfPropertyChange(() => MetroThemeBaseColour);
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public MetroThemeAccentColour? MetroThemeAccentColour
        {
            get { return metroThemeAccentColour; }
            set
            {
                if (value == metroThemeAccentColour) return;
                metroThemeAccentColour = value;
                NotifyOfPropertyChange(() => MetroThemeAccentColour);
            }
        }

        [JsonProperty]
        public StreamQuality DefaultStreamQuality
        {
            get { return defaultStreamQuality; }
            set
            {
                if (value == defaultStreamQuality) return;
                defaultStreamQuality = value;
                NotifyOfPropertyChange(() => DefaultStreamQuality);
            }
        }

        [DefaultValue(DEFAULT_LIVESTREAMER_FULL_PATH)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string LivestreamerFullPath
        {
            get { return livestreamerFullPath; }
            set
            {
                if (value == livestreamerFullPath) return;
                livestreamerFullPath = value;
                NotifyOfPropertyChange(() => LivestreamerFullPath);
            }
        }

        [DefaultValue(DEFAULT_CHROME_FULL_PATH)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string ChromeFullPath
        {
            get { return chromeFullPath; }
            set
            {
                if (value == chromeFullPath) return;
                chromeFullPath = value;
                NotifyOfPropertyChange(() => ChromeFullPath);
            }
        }

        [DefaultValue(DEFAULT_MINIMUM_EVENT_VIEWERS)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MinimumEventViewers
        {
            get { return minimumEventViewers; }
            set
            {
                if (value == minimumEventViewers) return;
                minimumEventViewers = value;
                NotifyOfPropertyChange(() => MinimumEventViewers);
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool DisableNotifications
        {
            get { return disableNotifications; }
            set
            {
                if (value == disableNotifications) return;
                disableNotifications = value;
                NotifyOfPropertyChange(() => DisableNotifications);
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool HideStreamOutputMessageBoxOnLoad
        {
            get { return hideStreamOutputMessageBoxOnLoad; }
            set
            {
                if (value == hideStreamOutputMessageBoxOnLoad) return;
                hideStreamOutputMessageBoxOnLoad = value;
                NotifyOfPropertyChange(() => HideStreamOutputMessageBoxOnLoad);
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool PassthroughClientId
        {
            get { return passthroughClientId; }
            set
            {
                if (value == passthroughClientId) return;
                passthroughClientId = value;
                NotifyOfPropertyChange(() => PassthroughClientId);
            }
        }

        /// <summary>
        /// Channel names in this collection should not raise notifications. <para/>
        /// We store these in settings so it can apply to both monitored and popular streams.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ExcludeNotifyConverter))]
        public ObservableCollection<UniqueStreamKey> ExcludeFromNotifying { get; } = new ObservableCollection<UniqueStreamKey>();
    }

    /// <summary>
    /// Migrates from the old array of streamids format to the new format using <see cref="UniqueStreamKey"/> type
    /// </summary>
    public class ExcludeNotifyConverter : JsonConverter
    {
        public static bool SaveRequired;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jsonObj = JArray.FromObject(value);
            jsonObj.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            
            var exclusions = (ObservableCollection<UniqueStreamKey>) existingValue;
            
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.EndArray:
                        return exclusions;
                    case JsonToken.StartObject:
                        var excludeNotify = serializer.Deserialize<UniqueStreamKey>(reader);
                        exclusions.Add(excludeNotify);
                        break;
                    default: // convert old array of stream ids
                        var streamId = reader.Value.ToString();
                        SaveRequired = true; // if we ran conversions then we should save the new output file
                        exclusions.Add(new UniqueStreamKey(TwitchApiClient.API_NAME, streamId));
                        break;
                }
            }

            return exclusions;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}