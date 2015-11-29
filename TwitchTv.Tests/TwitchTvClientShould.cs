﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TwitchTv.Tests
{
    public class TwitchTvClientShould
    {
        private readonly TwitchTvReadonlyClient sut = new TwitchTvReadonlyClient();

        [Fact]
        public async Task GetFollowsFromUser()
        {
            var followedStreams = await sut.GetUserFollows("fxfighter");
            Assert.NotNull(followedStreams);
            Assert.NotEmpty(followedStreams.Follows);
        }

        [Fact]
        public async Task GetChannelDetails()
        {
            var channelDetails = await sut.GetChannelDetails("fxfighter");
            Assert.NotNull(channelDetails);
        }

        [Fact]
        public async Task GetStreamDetails()
        {
            var streamDetails = await sut.GetStreamDetails("etup");
            Assert.NotNull(streamDetails);
        }

        [Fact]
        public async Task GetStreamsDetails()
        {
            var streamNames = new List<string>(new []
            {
                "massansc",
                "esl_csgo",
                "saintvicious"
            });
            var streamsDetails = await sut.GetStreamsDetails(streamNames);
            Assert.NotNull(streamsDetails);
            Assert.NotEmpty(streamsDetails);
        }

        [Fact]
        public async Task GetTopGamesList()
        {
            var topGames = await sut.GetTopGames();
            Assert.NotNull(topGames);
            Assert.NotEmpty(topGames);
        }

        [Fact]
        public async Task FindStreamEtup()
        {
            var streamsResult = await sut.SearchStreams("Etup");
            Assert.NotNull(streamsResult);
            Assert.NotEmpty(streamsResult);
        }

        [Fact]
        public async Task FindStreamsGivenPartialNames()
        {
            var streamsResult = await sut.SearchStreams("C9");
            Assert.NotNull(streamsResult);
            Assert.NotEmpty(streamsResult);
        }

        [Fact]
        public async Task FindGameMinecraft()
        {
            var gamesResult = await sut.SearchGames("Minecraft");
            Assert.NotNull(gamesResult);
            Assert.NotEmpty(gamesResult);
        }

        [Fact]
        public async Task FindGameWoW()
        {
            var gamesResult = await sut.SearchGames("World of Warcraft");
            Assert.NotNull(gamesResult);
            Assert.NotEmpty(gamesResult);
        }

        [Fact]
        public async Task GetTopStreams()
        {
            var topStreams = await sut.GetTopStreams();
            Assert.NotNull(topStreams);
            Assert.NotEmpty(topStreams);
        }

        [InlineData("World of Warcraft")]
        [InlineData("Minecraft")]
        [InlineData("League of Legends")]
        [Theory]
        public async Task GetTopStreamsByGame(string gameName)
        {
            var topGameStreams = await sut.GetTopStreamsByGame(gameName);
            Assert.NotNull(topGameStreams);
            Assert.NotEmpty(topGameStreams);
        }
    }
}
