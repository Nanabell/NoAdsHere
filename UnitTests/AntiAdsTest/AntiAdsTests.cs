using System;
using System.Collections.Generic;
using System.Linq;
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
using NoAdsHere.Services.AntiAds;
using NoAdsHere.Services.Database;
using NUnit.Framework;

namespace UnitTests.AntiAdsTest
{
    [TestFixture]
    public class AntiAdsTests
    {
        private IServiceProvider _provider;
        private DiscordShardedClient _client;
        private MongoClient _mongo;
        private ITextChannel TestChannel => _client.GetChannel(336769094902611968) as ITextChannel;
        private IGuild TestGuild => _client.GetGuild(173334405438242816);
        private IGuildUser TestUser => TestGuild.GetCurrentUserAsync().GetAwaiter().GetResult();

        [OneTimeSetUp]
        public async Task Init()
        {
            _client = await DiscordClient.GetClientAsync();
            _mongo = new MongoClient();
            _provider = new ServiceCollection()
                .AddSingleton(new DatabaseService(_mongo, "Test"))
                .AddSingleton(_client)
                .BuildServiceProvider();

            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            };
            ConventionRegistry.Register("EnumStringConvention", pack, t => true);
        }

        [Test]
        public void InstallAntiAds()
        {
            Assert.DoesNotThrowAsync(async () => await AntiAds.Install(_provider));
        }

        [Test]
        [TestCase(BlockType.InstantInvite)]
        public async Task EnableOnce(BlockType type)
        {
            Assert.IsTrue(await AntiAds.TryEnableGuild(1, type));
        }

        [Test]
        public async Task DisableOnce()
        {
            Assert.IsTrue(await AntiAds.TryDisableGuild(1, BlockType.InstantInvite));
        }

        [Test]
        public async Task EnableAlreadyEnabled()
        {
            Assert.IsTrue(await AntiAds.TryEnableGuild(1, BlockType.InstantInvite));
            Assert.IsFalse(await AntiAds.TryEnableGuild(1, BlockType.InstantInvite));
        }

        [Test]
        public async Task DisableNotEnabled([Values] BlockType type)
        {
            if (type == BlockType.All)
                Assert.Pass();
            await AntiAds.TryEnableGuild(1, BlockType.All);
            Assert.IsFalse(await AntiAds.TryDisableGuild(1, type));
        }

        [Test]
        public async Task IsActiveTest([Values] BlockType type)
        {
            Assert.IsTrue(await AntiAds.TryEnableGuild(1, type));
            Assert.IsTrue(AntiAds.IsActive(1, type));
        }

        [Test]
        public void IsActiveNotActiveTest([Values] BlockType type)
        {
            Assert.IsFalse(AntiAds.IsActive(1, type));
        }

        [Test]
        public void InviteMatch()
        {
            Assert.IsTrue(AntiAds.IsRegexMatch(AntiAds.InstantInvite, @"DiSCorD.gG/InValIdInVITe"));
        }

        [Test]
        public void TwitchStreamMatch()
        {
            Assert.IsTrue(AntiAds.IsRegexMatch(AntiAds.TwitchStream, @"twitch.tv/SomeRandonUserName"));
        }

        [Test]
        public void TwitchVideoMatch()
        {
            Assert.IsTrue(AntiAds.IsRegexMatch(AntiAds.TwitchVideo, @"TwiTch.Tv/ViDeOS/001122334455"));
        }

        [Test]
        public void TwitchClipMatch()
        {
            Assert.IsTrue(AntiAds.IsRegexMatch(AntiAds.TwitchClip, @"ClIps.twitch.tv/SomeEmoteNames"));
        }

        [Test]
        public void YoutubeLinkMatch()
        {
            Assert.IsTrue(AntiAds.IsRegexMatch(AntiAds.YoutubeLink, @"yoUTube.COm/waTcH?v=_tauIVy6RFc"));
        }

        [Test]
        public void SteamScamMatch()
        {
            Assert.IsTrue(AntiAds.IsRegexMatch(AntiAds.SteamScam, @"sTeaMsuMMeR.COm/?iD=SteamId"));
        }

        private async Task PrepareIsToDelete(BlockType type)
        {
            InstallAntiAds();
            await EnableOnce(type);
        }

        [Test]
        public async Task IsToDeleteTestNoIgnores([Values] BlockType type)
        {
            await PrepareIsToDelete(type);
            Assert.IsTrue(await AntiAds.IsToDelete(TestChannel, TestUser, GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDeleteNullUser()
        {
            await PrepareIsToDelete(BlockType.All);
            Assert.IsFalse(await AntiAds.IsToDelete(TestChannel, null, GetBlockExamples()[BlockType.All]));
        }

        [Test]
        public async Task IsToDeleteNullChannel()
        {
            await PrepareIsToDelete(BlockType.All);
            Assert.IsFalse(await AntiAds.IsToDelete(null, TestUser, GetBlockExamples()[BlockType.All]));
        }

        [Test]
        public async Task IsToDeleteNullOrEmptyMessage()
        {
            await PrepareIsToDelete(BlockType.All);
            Assert.IsFalse(await AntiAds.IsToDelete(TestChannel, TestUser, null));
        }

        [Test]
        public async Task IsToDeleteIgnoreMaster([Values] BlockType type)
        {
            await PrepareIsToDelete(type);
            var collection = _mongo.GetDatabase("Test").GetCollection<Master>();
            await collection.InsertOneAsync(new Master(TestUser.Id));
            Assert.IsFalse(await AntiAds.IsToDelete(TestChannel, TestUser, GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDeleteWithIgnores([Values] BlockType type, [Values] IgnoreType ignoreType)
        {
            await PrepareIsToDelete(type);
            ulong id = 0;
            switch (ignoreType)
            {
                case IgnoreType.User:
                    id = TestUser.Id;
                    break;

                case IgnoreType.Role:
                    id = TestUser.RoleIds.First();
                    break;

                case IgnoreType.Channel:
                    id = TestChannel.Id;
                    break;
            }

            var collection = _mongo.GetDatabase("Test").GetCollection<Ignore>();
            await collection.InsertOneAsync(new Ignore(TestGuild.Id, ignoreType, id, type));
            Assert.IsFalse(await AntiAds.IsToDelete(TestChannel, TestUser, GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDelteAllowedString([Values] BlockType type, [Values] IgnoreType ignoreType)
        {
            await PrepareIsToDelete(type);
            ulong id = 0;
            switch (ignoreType)
            {
                case IgnoreType.User:
                    id = TestUser.Id;
                    break;

                case IgnoreType.Role:
                    id = TestUser.RoleIds.First();
                    break;

                case IgnoreType.Channel:
                    id = TestChannel.Id;
                    break;
            }

            var collection = _mongo.GetDatabase("Test").GetCollection<AllowString>();
            await collection.InsertOneAsync(new AllowString(TestGuild.Id, ignoreType, id, GetBlockExamples()[type]));
            Assert.IsFalse(await AntiAds.IsToDelete(TestChannel, TestUser, GetBlockExamples()[type]));
        }

        [TearDown]
        public void Reset()
        {
            _mongo.DropDatabase("Test");
            AntiAds.ActiveBlocks.Clear();
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