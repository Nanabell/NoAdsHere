using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NoAdsHere.Services.Database;
using NoAdsHere.Services.FAQ;
using NUnit.Framework;

namespace UnitTests.FaqSystemTest
{
    [TestFixture]
    public class FaqSystemTests
    {
        private FaqSystem _faqSystem;
        private MongoClient _mongo;
        private ulong TestChannel => 336769094902611968;
        private static ulong TestGuild => 173334405438242816;
        private static ulong TestUser => 206813496585748480;

        [OneTimeSetUp]
        public void Init()
        {
            _mongo = new MongoClient();
            _faqSystem = new FaqSystem(new DatabaseService(_mongo, "Test"));
        }

        [Test]
        [TestCase("UnitTest")]
        public async Task AddGuildEntry(string name)
        {
            Assert.IsTrue(await _faqSystem.AddGuildEntryAsync(TestGuild, TestUser, name, "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddSameGuildEntry()
        {
            Assert.IsTrue(await _faqSystem.AddGuildEntryAsync(TestGuild, TestUser, "UnitTest", "This is a Faq Entry from UnitTesting"));
            Assert.IsFalse(await _faqSystem.AddGuildEntryAsync(TestGuild, TestUser, "UnitTest", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public void AddGuildEntryEmptyName()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGuildEntryAsync(TestGuild, TestUser, "", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public void AddGuildEntryEmptyContent()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGuildEntryAsync(TestGuild, TestUser, "UnitTest", ""));
        }

        [Test]
        [TestCase("GlobalUnitTest")]
        public async Task AddGlobalEntry(string name)
        {
            Assert.IsTrue(await _faqSystem.AddGlobalEntryAsync(TestUser, name, "This is a Global Faq Entry from UnitTesting"));
        }

        [Test]
        public async Task AddSameGlobalEntry()
        {
            Assert.IsTrue(await _faqSystem.AddGlobalEntryAsync(TestUser, "GlobalUnitTest", "This is a Faq Entry from UnitTesting"));
            Assert.IsFalse(await _faqSystem.AddGlobalEntryAsync(TestUser, "GlobalUnitTest", "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public void AddGlobalEntryEmptyName()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGlobalEntryAsync(TestUser, null, "This is a Faq Entry from UnitTesting"));
        }

        [Test]
        public void AddGlobalEntryEmptyContent()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _faqSystem.AddGlobalEntryAsync(TestUser, "UnitTest", ""));
        }

        [Test]
        public async Task GetEntry()
        {
            await AddGuildEntry("UnitTest");
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestGuild, "UnitTest");
            Assert.NotNull(entry);
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

            var entries = await _faqSystem.GetGuildEntriesAsync(TestGuild);
            Assert.AreEqual(2, entries.Count);
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
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestGuild, "UnitTest");
            Assert.IsNull(entry);
        }

        [Test]
        public async Task GetGlobalNotExistingEntry()
        {
            var entry = await _faqSystem.GetGlobalFaqEntryAsync("GlobalUnitTest");
            Assert.IsNull(entry);
        }

        [Test]
        public async Task RemoveEntry()
        {
            await AddGuildEntry("UnitTest");
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestGuild, "UnitTest");
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
            var entry = await _faqSystem.GetGuildFaqEntryAsync(TestGuild, "UnitTest");
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
            var similars = await _faqSystem.GetSimilarGuildEntries(TestGuild, "UnitTestt");
            Assert.NotZero(similars.Count);
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
            var similars = await _faqSystem.GetSimilarGuildEntries(TestGuild, "");
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
        }
    }
}