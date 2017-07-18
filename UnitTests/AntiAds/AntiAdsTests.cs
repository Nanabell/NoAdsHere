using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
        private DiscordShardedClient _client;
        private MongoClient _mongo;
        private ITextChannel TestChannel => _client.GetChannel(336769094902611968) as ITextChannel;

        [OneTimeSetUp]
        public async Task Init()
        {
            var ready = false;
            _client = new DiscordShardedClient();
            _mongo = new MongoClient();
            await _client.LoginAsync(TokenType.Bot, Config.Load().Token);
            await _client.StartAsync();
            _provider = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(new DatabaseService(_mongo, "Test"))
                .BuildServiceProvider();
            _client.Shards.First().Ready += () =>
            {
                ready = true;
                return Task.CompletedTask;
            };

            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            };

            ConventionRegistry.Register("EnumStringConvention", pack, t => true);

            while (!ready)
                await Task.Delay(25);
        }

        [Test]
        public void Install()
        {
            Assert.DoesNotThrowAsync(async () => await NoAdsHere.Services.AntiAds.AntiAds.Install(_provider));
        }

        [Test]
        public void Start()
        {
            Assert.DoesNotThrowAsync(async () => await NoAdsHere.Services.AntiAds.AntiAds.StartAsync());
        }

        [Test]
        public void Stop()
        {
            Assert.DoesNotThrowAsync(async () => await NoAdsHere.Services.AntiAds.AntiAds.StopAsync());
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

        [Test]
        public async Task DeleteMessageTest()
        {
            Assert.IsNotNull(TestChannel);

            var msg = await TestChannel.SendMessageAsync("This is a UnitTestMessage, TTL `5sec`");
            var context = new CommandContext(_client, msg);
            Assert.DoesNotThrowAsync(async () => await NoAdsHere.Services.AntiAds.AntiAds.DeleteMessage(context, "Message was a UnitTest"));
        }

        private async Task<IGuildUser> PrepareIsToDelete(BlockType type)
        {
            Install();
            await EnableOnce(type);
            return await TestChannel.Guild.GetCurrentUserAsync();
        }

        [Test]
        public async Task IsToDeleteTestNoIgnores([Values] BlockType type)
        {
            var me = await PrepareIsToDelete(type);
            Assert.IsTrue(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestChannel, me, GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDeleteIgnoreMaster([Values] BlockType type)
        {
            var me = await PrepareIsToDelete(type);
            var collection = _mongo.GetDatabase("Test").GetCollection<Master>();
            await collection.InsertOneAsync(new Master(me.Id));
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestChannel, me, GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDeleteWithIgnores([Values] BlockType type, [Values] IgnoreType ignoreType)
        {
            var me = await PrepareIsToDelete(type);
            ulong id = 0;
            switch (ignoreType)
            {
                case IgnoreType.User:
                    id = me.Id;
                    break;

                case IgnoreType.Role:
                    id = TestChannel.GuildId;
                    break;

                case IgnoreType.Channel:
                    id = TestChannel.Id;
                    break;
            }

            var collection = _mongo.GetDatabase("Test").GetCollection<Ignore>();
            await collection.InsertOneAsync(new Ignore(TestChannel.GuildId, ignoreType, id, type));
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestChannel, me, GetBlockExamples()[type]));
        }

        [Test]
        public async Task IsToDelteAllowedString([Values] BlockType type, [Values] IgnoreType ignoreType)
        {
            var me = await PrepareIsToDelete(type);
            ulong id = 0;
            switch (ignoreType)
            {
                case IgnoreType.User:
                    id = me.Id;
                    break;

                case IgnoreType.Role:
                    id = TestChannel.GuildId;
                    break;

                case IgnoreType.Channel:
                    id = TestChannel.Id;
                    break;
            }

            var collection = _mongo.GetDatabase("Test").GetCollection<AllowString>();
            await collection.InsertOneAsync(new AllowString(TestChannel.GuildId, ignoreType, id, GetBlockExamples()[type]));
            Assert.IsFalse(await NoAdsHere.Services.AntiAds.AntiAds.IsToDelete(TestChannel, me, GetBlockExamples()[type]));
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
            await _client.StopAsync();
            await _client.LogoutAsync();
            _client.Dispose();
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