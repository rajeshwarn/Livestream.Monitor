﻿using System;
using Caliburn.Micro;
using Livestream.Monitor.Model.StreamProviders;

namespace Livestream.Monitor.Model.Monitoring
{
    public class LivestreamModel : PropertyChangedBase
    {
        private DateTimeOffset startTime;
        private long viewers;
        private string game;
        private string description;
        private string displayName;
        private bool live;
        private bool isPartner;
        private ThumbnailUrls thumbnailUrls;
        private bool dontNotify;
        private DateTimeOffset? lastLiveTime;
        private string broadcasterLanguage;
        private string language;

        /// <summary> The unique identifier for the livestream </summary>
        public string Id { get; set; }
        
        public IStreamProvider StreamProvider { get; set; }

        public bool Live
        {
            get { return live; }
            set
            {
                if (value == live) return;
                live = value;
                NotifyOfPropertyChange(() => Live);
                NotifyOfPropertyChange(() => Uptime);

                // must update after notifying property change to give time to inspect LastLiveTime property before live state changed
                if (live)
                    LastLiveTime = DateTimeOffset.Now;
            }
        }

        public string DisplayName
        {
            get { return displayName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(DisplayName));
                if (value == displayName) return;
                displayName = value;
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                if (value == description) return;
                description = value;
                NotifyOfPropertyChange(() => Description);
            }
        }

        public string Game
        {
            get { return game; }
            set
            {
                if (value == game) return;
                game = value;
                NotifyOfPropertyChange(() => Game);
            }
        }

        public long Viewers
        {
            get { return viewers; }
            set
            {
                if (value == viewers) return;
                viewers = value;
                NotifyOfPropertyChange(() => Viewers);
            }
        }

        public DateTimeOffset StartTime
        {
            get { return startTime; }
            set
            {
                if (value.Equals(startTime)) return;
                startTime = value;
                NotifyOfPropertyChange(() => StartTime);
                NotifyOfPropertyChange(() => Uptime);
            }
        }

        public bool IsPartner
        {
            get { return isPartner; }
            set
            {
                if (value == isPartner) return;
                isPartner = value;
                NotifyOfPropertyChange();
            }
        }

        public ThumbnailUrls ThumbnailUrls
        {
            get { return thumbnailUrls; }
            set
            {
                if (Equals(value, thumbnailUrls)) return;
                thumbnailUrls = value;
                NotifyOfPropertyChange(() => ThumbnailUrls);
            }
        }
        
        public string BroadcasterLanguage
        {
            get { return broadcasterLanguage; }
            set
            {
                if (value == broadcasterLanguage) return;
                broadcasterLanguage = value;
                NotifyOfPropertyChange(() => BroadcasterLanguage);
            }
        }

        public string Language
        {
            get { return language; }
            set
            {
                if (value == language) return;
                language = value;
                NotifyOfPropertyChange(() => Language);
            }
        }

        public string StreamUrl => StreamProvider?.GetStreamUrl(Id);

        public string ChatUrl => StreamProvider?.GetChatUrl(Id);

        /// <summary> The username this livestream came from via importing (twitch allows importing followed streams) </summary>
        public string ImportedBy { get; set; }

        public TimeSpan Uptime => Live ? DateTimeOffset.Now - StartTime : TimeSpan.Zero;

        public DateTimeOffset? LastLiveTime
        {
            get { return lastLiveTime; }
            set
            {
                if (value.Equals(lastLiveTime)) return;
                lastLiveTime = value;
                NotifyOfPropertyChange(() => LastLiveTime);
            }
        }

        /// <summary> Exclude this livestream from raising popup notifications </summary>
        public bool DontNotify
        {
            get { return dontNotify; }
            set
            {
                if (value == dontNotify) return;
                dontNotify = value;
                NotifyOfPropertyChange(() => DontNotify);
            }
        }

        /// <summary> Sets the livestream to the offline state </summary>
        public void Offline()
        {
            Live = false;
            Viewers = 0;
            StartTime = DateTimeOffset.MinValue;
        }

        public override string ToString() => 
            $"{displayName}, Viewers={viewers}, Uptime={Uptime.ToString("hh'h 'mm'm 'ss's'")}";

        #region Equality members

        protected bool Equals(LivestreamModel other)
        {
            return string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LivestreamModel) obj);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        #endregion
    }
}
