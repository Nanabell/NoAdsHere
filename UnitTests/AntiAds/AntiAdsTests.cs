using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.Global;
using NoAdsHere.Database.Models.Guild;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Services.Database;
using NUnit.Framework;

namespace UnitTests.AntiAds
{
    [TestFixture]
    public class AntiAdsTests
    {
        private IServiceProvider _provider;
        private MongoClient _mongo;
        private ulong TestChannel => 336769094902611968;
        private static ulong TestGuild => 173334405438242816;
        private static ulong TestUser => 206813496585748480;

        [OneTimeSetUp]
        public async Task Init()
        {
            var ready = false;
            var client = new DiscordSocketClient();
            var vars = Environment.GetEnvironmentVariables();
            var token = vars.Contains("APPVEYOR") ? Environment.GetEnvironmentVariable("BOT_TOKEN") : Config.Load().Token;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            _mongo = new MongoClient();
            _provider = new ServiceCollection()
                .AddSingleton(new DatabaseService(_mongo, "Test"))
                .AddSingleton(new DiscordShardedClient())
                .BuildServiceProvider();

            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            };
            ConventionRegistry.Register("EnumStringConvention", pack, t => true);

            client.Ready += () =>
            {
                ready = true;
                return Task.CompletedTask;
            };

            while (!ready)
            {
                await Task.Delay(25);
            }
        }

        [Test]
        public void Install()
        {
            Assert.DoesNotThrowAsync(async () => await NoAdsHere.Services.AntiAds.AntiAds.Install(_provider));
        }

        [Test]
        [TestCase(BlockType.InstantInvite)]
        public async Task EnableOnce(BlockType type)
        {
            Assert.IsTrue(await NoAdsHere.Services.AntiAds.AntiAds.TryEnableGuild(1, type));
        }

        [Test]
        public async Task DisableOnce()
        {
            Assert.IsTrue(await NoAdsHere.Services.AntiAds.AntiAds.TryDisableGuild(1, BlockType.InstantInvite));
        }

        [Test]
        public async Task EnableAlreadyEnabled()
        {
            Assert.IsTrue(await NoAdsHere.Services.AntiAds.AntiAds.TryEnableGuild(1, BlockType.InstantInvite));
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.TryEnableGuild(1, BlockType.InstantInvite));
        }

        [Test]
        public async Task DisableNotEnabled([Values] BlockType type)
        {
            if (type == BlockType.All)
                Assert.Pass();
            await NoAdsHere.Services.AntiAds.AntiAds.TryEnableGuild(1, BlockType.All);
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.TryDisableGuild(1, type));
        }

        [Test]
        public async Task IsActiveTest([Values] BlockType type)
        {
            Assert.IsTrue(await NoAdsHere.Services.AntiAds.AntiAds.TryEnableGuild(1, type));
            Assert.IsTrue(NoAdsHere.Services.AntiAds.AntiAds.IsActive(1, type));
        }

        [Test]
        public void IsActiveNotActiveTest([Values] BlockType type)
        {
            Assert.IsFalse(NoAdsHere.Services.AntiAds.AntiAds.IsActive(1, type));
        }

        [Test]
        public void InviteMatch()
        {
            Assert.IsTrue(NoAdsHere.Services.AntiAds.AntiAds.IsRegexMatch(NoAdsHere.Services.AntiAds.AntiAds.InstantInvite, @"DiSCorD.gG/InValIdInVITe"));
        }

        [Test]
        public void TwitchStreamMatch()
        {
            Assert.IsTrue(NoAdsHere.Services.AntiAds.AntiAds.IsRegexMatch(NoAdsHere.Services.AntiAds.AntiAds.TwitchStream, @"twitch.tv/SomeRandonUserName"));
        }

        [Test]
        public void TwitchVideoMatch()
        {
            Assert.IsTrue(NoAdsHere.Services.AntiAds.AntiAds.IsRegexMatch(NoAdsHere.Services.AntiAds.AntiAds.TwitchVideo, @"TwiTch.Tv/ViDeOS/001122334455"));
        }

        [Test]
        public void TwitchClipMatch()
        {
            Assert.IsTrue(NoAdsHere.Services.AntiAds.AntiAds.IsRegexMatch(NoAdsHere.Services.AntiAds.AntiAds.TwitchClip, @"ClIps.twitch.tv/SomeEmoteNames"));
        }

        [Test]
        public void YoutubeLinkMatch()
        {
            Assert.IsTrue(NoAdsHere.Services.AntiAds.AntiAds.IsRegexMatch(NoAdsHere.Services.AntiAds.AntiAds.YoutubeLink, @"yoUTube.COm/waTcH?v=_tauIVy6RFc"));
        }

        [Test]
        public void SteamScamMatch()
        {
            Assert.IsTrue(NoAdsHere.Services.AntiAds.AntiAds.IsRegexMatch(NoAdsHere.Services.AntiAds.AntiAds.SteamScam, @"sTeaMsuMMeR.COm/?iD=SteamId"));
        }

        private async Task PrepareIsToDelete(BlockType type)
        {
            Install();
            await EnableOnce(type);
        }

        private IEnumerable<ulong> GetRoleIds()
        {
            return new List<ulong>()
            {
                TestGuild
            };
        }

        [Test]
        public async Task IsToDeleteTestNoIgnores([Values] BlockType type)
        {
            await PrepareIsToDelete(type);
            Assert.IsTrue(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestGuild, TestChannel, TestUser, GetRoleIds(), GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDeleteIgnoreMaster([Values] BlockType type)
        {
            await PrepareIsToDelete(type);
            var collection = _mongo.GetDatabase("Test").GetCollection<Master>();
            await collection.InsertOneAsync(new Master(TestUser));
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestGuild, TestChannel, TestUser, GetRoleIds(), GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDeleteWithIgnores([Values] BlockType type, [Values] IgnoreType ignoreType)
        {
            await PrepareIsToDelete(type);
            ulong id = 0;
            switch (ignoreType)
            {
                case IgnoreType.User:
                    id = TestUser;
                    break;

                case IgnoreType.Role:
                    id = TestGuild;
                    break;

                case IgnoreType.Channel:
                    id = TestChannel;
                    break;
            }

            var collection = _mongo.GetDatabase("Test").GetCollection<Ignore>();
            await collection.InsertOneAsync(new Ignore(TestGuild, ignoreType, id, type));
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestGuild, TestChannel, TestUser, GetRoleIds(), GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDelteAllowedString([Values] BlockType type, [Values] IgnoreType ignoreType)
        {
            await PrepareIsToDelete(type);
            ulong id = 0;
            switch (ignoreType)
            {
                case IgnoreType.User:
                    id = TestUser;
                    break;

                case IgnoreType.Role:
                    id = TestGuild;
                    break;

                case IgnoreType.Channel:
                    id = TestChannel;
                    break;
            }

            var collection = _mongo.GetDatabase("Test").GetCollection<AllowString>();
            await collection.InsertOneAsync(new AllowString(TestGuild, ignoreType, id, GetBlockExamples()[type]));
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestGuild, TestChannel, TestUser, GetRoleIds(), GetBlockExamples()[type]));
        }

        [TearDown]
        public void Reset()
        {
            _mongo.DropDatabase("Test");
            NoAdsHere.Services.AntiAds.AntiAds.ActiveBlocks.Clear();
        }

        [OneTimeTearDown]
        public async Task Shutdown()
        {
            await _mongo.DropDatabaseAsync("Test");
        }

        private Dictionary<BlockType, string> GetBlockExamples()
        {
            var map = new Dictionary<BlockType, string>
            {
                {BlockType.InstantInvite, "discord.gg/InvalidInvite"},
                {BlockType.YoutubeLink, "yoUTube.COm/waTcH?v=_tauIVy6RFc"},
                {BlockType.TwitchStream, "twitch.tv/SomeRandonUserName" },
                {BlockType.TwitchVideo, "TwiTch.Tv/ViDeOS/001122334455" },
                {BlockType.TwitchClip, "ClIps.twitch.tv/SomeEmoteNames" },
                {BlockType.SteamScam, "sTeaMsuMMeR.COm/?iD=SteamId" },
                {BlockType.All, "discord.gg/InvalidInvite" }
            };

            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                if (map.ContainsKey(type))
                {
                    if (map[type] != (string.Empty))
                    {
                        continue;
                    }
                }
                throw new InvalidOperationException($"An Example for the Blocktype {type} is missing please add and rerun!");
            }
            return map;
        }
    }
}