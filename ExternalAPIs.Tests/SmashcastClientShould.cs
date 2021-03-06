﻿using System.Threading.Tasks;
using ExternalAPIs.Smashcast;
using ExternalAPIs.Smashcast.Query;
using Xunit;

namespace ExternalAPIs.Tests
{
    public class SmashcastClientShould
    {
        private const string KnownChannelName = "rewardsgg";
        private const int KnownChannelId = 859185;

        //private const string KnownChannelName = "Heroesofcards";
        //private const int KnownChannelId = 217859;

        private readonly SmashcastReadonlyClient sut = new SmashcastReadonlyClient();

        [Fact]
        public async Task GetTopStreams()
        {
            var topStreamsQuery = new TopStreamsQuery();
            var livestreams = await sut.GetTopStreams(topStreamsQuery);
            Assert.NotNull(livestreams);
            Assert.NotEmpty(livestreams);
        }

        [Fact]
        public async Task GetLivestreamDetails()
        {
            var mediainfo = await sut.GetStreamDetails(KnownChannelId.ToString());
            Assert.NotNull(mediainfo);
        }

        [Fact]
        public async Task GetChannelDetails()
        {
            var livestream = await sut.GetChannelDetails(KnownChannelName);
            Assert.NotNull(livestream);
            Assert.NotNull(livestream.Channel);
        }

        [Fact]
        public async Task GetChannelVideos()
        {
            var channelVideosQuery = new ChannelVideosQuery("ECTVLoL");
            var videos = await sut.GetChannelVideos(channelVideosQuery);
            Assert.NotNull(videos);
            Assert.NotEmpty(videos);
            Assert.NotNull(videos[0].Channel);
        }

        [Fact]
        public async Task GetUserFollows()
        {
            var followings = await sut.GetUserFollows("fxfighter");
            Assert.NotNull(followings);
            Assert.NotEmpty(followings);
        }
        
        [Fact]
        public async Task GetTopGames()
        {
            var topGames = await sut.GetTopGames();
            Assert.NotNull(topGames);
            Assert.NotEmpty(topGames);
        }
    }
}
