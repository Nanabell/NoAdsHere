using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MongoDB.Driver;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Services.Database;
using NoAdsHere.Services.FAQ;
using NUnit.Framework;

namespace UnitTests.FaqSystemTest
{
    [TestFixture]
    public class FaqSystemTests
    {
        private DiscordShardedClient _client;
        private FaqSystem _faqSystem;
        private MongoClient _mongo;
        private ITextChannel TestChannel => _client.GetChannel(336769094902611968) as ITextChannel;

        [OneTimeSetUp]
        public async Task Init()
        {
            _mongo = new MongoClient();
            _faqSystem = new FaqSystem(new DatabaseService(_mongo, "Test"), Config.Load());

            var ready = false;
            _client = new DiscordShardedClient();
            await _client.LoginAsync(TokenType.Bot, Config.Load().Token);
            await _client.StartAsync();

            _client.Shards.First().Ready += () =>
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
        [TestCase("UnitTest")]
        public async Task AddGuildEntry(string name)
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.IsTrue(await _faqSystem.AddGuildEntryAsync(TestChannel.Guild, me, name, "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddSameGuildEntry()
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.IsTrue(await _faqSystem.AddGuildEntryAsync(TestChannel.Guild, me, "UnitTest", "This is a Faq Entry from UnitTesting"));
            Assert.IsFalse(await _faqSystem.AddGuildEntryAsync(TestChannel.Guild, me, "UnitTest", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddGuildEntryEmptyName()
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGuildEntryAsync(TestChannel.Guild, me, "", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddGuildEntryEmptyContent()
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGuildEntryAsync(TestChannel.Guild, me, "UnitTest", ""));
        }

        [Test]
        [TestCase("GlobalUnitTest")]
        public async Task AddGlobalEntry(string name)
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.IsTrue(await _faqSystem.AddGlobalEntryAsync(me, name, "This is a Global Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddSameGlobalEntry()
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.IsTrue(await _faqSystem.AddGlobalEntryAsync(me, "GlobalUnitTest", "This is a Faq Entry from UnitTesting"));
            Assert.IsFalse(await _faqSystem.AddGlobalEntryAsync(me, "GlobalUnitTest", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddGlobalEntryEmptyName()
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGlobalEntryAsync(me, null, "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddGlobalEntryEmptyContent()
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGlobalEntryAsync(me, "UnitTest", ""));
        }

        [Test]
        public async Task GetEntry()
        {
            await AddGuildEntry("UnitTest");
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestChannel.Guild, "UnitTest");
            Assert.NotNull(entry);
        }

        [Test]
        public async Task GetEntryNullGuild()
        {
            await AddGuildEntry("UnitTest");
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.GetGuildFaqEntryAsync(null, "UnitTest"));
        }

        [Test]
        public async Task GetGlobalEntry()
        {
            await AddGlobalEntry("GlobalUnitTest");
            var entry = await _faqSystem.GetGlobalFaqEntryAsync("GlobalUnitTest");
            Assert.NotNull(entry);
        }

        [Test]
        public async Task GetEntries()
        {
            await AddGuildEntry("UnitTest");
            await AddGuildEntry("UnitTest2");

            var entries = await _faqSystem.GetGuildEntriesAsync(TestChannel.Guild);
            Assert.AreEqual(2, entries.Count);
        }

        [Test]
        public async Task GetEntriesNullGuild()
        {
            await AddGuildEntry("UnitTest");
            await AddGuildEntry("UnitTest2");

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.GetGuildEntriesAsync(null));
        }

        [Test]
        public async Task GetGlobalEntries()
        {
            await AddGlobalEntry("GlobalUnitTest");
            await AddGlobalEntry("GlobalUnitTest2");

            var entries = await _faqSystem.GetGlobalEntriesAsync();
            Assert.AreEqual(2, entries.Count);
        }

        [Test]
        public async Task GetNotExistingEntry()
        {
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestChannel.Guild, "UnitTest");
            Assert.IsNull(entry);
        }

        [Test]
        public async Task GetGlobalNotExistingEntry()
        {
            var entry = await _faqSystem.GetGlobalFaqEntryAsync("GlobalUnitTest");
            Assert.IsNull(entry);
        }

        [Test]
        public void AddEntryNullUser()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGuildEntryAsync(TestChannel.Guild, null, "UnitTest", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddEntryNullGuild()
        {
            var me = await TestChannel.Guild.GetCurrentUserAsync();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGuildEntryAsync(null, me, "UnitTest", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public void AddGlobalEntryNullUser()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGlobalEntryAsync(null, "UnitTest", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task RemoveEntry()
        {
            await AddGuildEntry("UnitTest");
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestChannel.Guild, "UnitTest");
            var result = await _faqSystem.RemoveGuildEntryAsync(entry);
            Assert.IsTrue(result.IsAcknowledged);
        }

        [Test]
        public async Task RemoveGlobalEntry()
        {
            await AddGlobalEntry("GlobalUnitTest");
            var entry = await _faqSystem.GetGlobalFaqEntryAsync("GlobalUnitTest");
            var result = await _faqSystem.RemoveGlobalEntryAsync(entry);
            Assert.IsTrue(result.IsAcknowledged);
        }

        [Test]
        public void RemoveNullEntry()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.RemoveGuildEntryAsync(null));
        }

        [Test]
        public void RemoveGlobalNullEntry()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.RemoveGlobalEntryAsync(null));
        }

        [Test]
        public async Task SaveEntry()
        {
            await AddGuildEntry("UnitTest");
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestChannel.Guild, "UnitTest");
            var result = await _faqSystem.SaveGuildEntryAsync(entry);
            Assert.IsTrue(result.IsAcknowledged);
        }

        [Test]
        public async Task SaveGlobalEntry()
        {
            await AddGlobalEntry("GlobalUnitTest");
            var entry = await _faqSystem.GetGlobalFaqEntryAsync("GlobalUnitTest");
            var result = await _faqSystem.SaveGlobalEntryAsync(entry);
            Assert.IsTrue(result.IsAcknowledged);
        }

        [Test]
        public void SaveNullEntry()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.SaveGuildEntryAsync(null));
        }

        [Test]
        public void SaveGlobalNullEntry()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.SaveGlobalEntryAsync(null));
        }

        [Test]
        public async Task GetSimilarEntries()
        {
            var response = "";
            foreach (var c in "UnitTest")
            {
                response += c;
                await AddGuildEntry(response);
            }
            var similars = await _faqSystem.GetSimilarGuildEntries(TestChannel.Guild, "UnitTestt");
            Assert.NotZero(similars.Count);
        }

        [Test]
        public async Task GetSimilarEntriesNullGuild()
        {
            var response = "";
            foreach (var c in "UnitTest")
            {
                response += c;
                await AddGuildEntry(response);
            }
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.GetSimilarGuildEntries(null, "UnitTestt"));
        }

        [Test]
        public async Task GetSimilarGlobalEntries()
        {
            var response = "";
            foreach (var c in "GlobalUnitTest")
            {
                response += c;
                await AddGlobalEntry(response);
            }
            var similars = await _faqSystem.GetSimilarGlobalEntries("GlobalUnitTestt");
            Assert.NotZero(similars.Count);
        }

        [Test]
        public async Task GetSimilarEntriesEmptyString()
        {
            var response = "";
            foreach (var c in "UnitTest")
            {
                response += c;
                await AddGuildEntry(response);
            }
            var similars = await _faqSystem.GetSimilarGuildEntries(TestChannel.Guild, "");
            Assert.NotZero(similars.Count);
        }

        [Test]
        public async Task GetSimilarGlobalEntriesEmptryString()
        {
            var response = "";
            foreach (var c in "GlobalUnitTest")
            {
                response += c;
                await AddGlobalEntry(response);
            }
            var similars = await _faqSystem.GetSimilarGlobalEntries("");
            Assert.NotZero(similars.Count);
        }

        [TearDown]
        public void TearDown()
        {
            _mongo.DropDatabase("Test");
        }

        [OneTimeTearDown]
        public async Task Stop()
        {
            await _mongo.DropDatabaseAsync("Test");
            await _client.StopAsync();
            await _client.LogoutAsync();
            _client.Dispose();
        }
    }
}