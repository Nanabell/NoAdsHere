using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NoAdsHere.Database.Entities;

namespace NoAdsHere.Database
{
    public class DatabaseContext : IDisposable
    {
        private const string GuildConfigPath = "GuildConfig/";
        private const string AdPath = "Ads/";
        private static readonly string BasePath = $"{Directory.GetCurrentDirectory()}/data/";

        /// <summary>
        /// Do not Load the GuildConfig at Startup 
        /// </summary>
        /// <param name="loadAdList">Should the Adlist be Loaded?</param>
        /// <param name="loadGuildConfig">Should the GuildConfig be Loaded? requires <paramref name="guildId"/> and <paramref name="createNewFile"/> </param>
        /// <param name="guildId">The GuildId of the GuildConfig. Only useful in conbination with <paramref name="loadGuildConfig"/> enabled</param>
        /// <param name="createNewFile">If the GuildConfig does not exist should it be written to disk aswell? Only useful in conbination with <paramref name="loadGuildConfig"/> enabled</param>
        public DatabaseContext(bool loadAdList = false, bool loadGuildConfig = false, ulong guildId = 0, bool createNewFile = true)
        {
            if (loadAdList)
                LoadAdList();

            if (loadGuildConfig)
                LoadGuildConfig(guildId, createNewFile);
        }

        public GuildConfig GuildConfig { get; private set; }
        public List<Ad> AdList { get; } = new List<Ad>();


        private static string GetAdPath(string name)
            => BasePath + AdPath + name + ".json";
        
        private static string GetGcPath(ulong guildId)
            => BasePath + GuildConfigPath + guildId + ".json";

        /// <summary>
        /// Load the Adlist this can be called in the Constructor
        /// </summary>
        public void LoadAdList()
        {
            if (!Directory.Exists(GetAdPath("")))
                Directory.CreateDirectory(GetAdPath(""));

            AdList.Clear();
            foreach (var path in Directory.EnumerateFiles(GetAdPath("")))
            {
                AdList.Add(JsonConvert.DeserializeObject<Ad>(File.ReadAllText(GetAdPath(path))));
            }
        }

        public void LoadGuildConfig(ulong guildId, bool createNewFile)
        {
            if (!Directory.Exists(GetGcPath(guildId)))
                Directory.CreateDirectory(GetGcPath(guildId));
            
            if (File.Exists(GetGcPath(guildId)))
            {
                GuildConfig = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(GetGcPath(guildId)));
                return;
            }
            GuildConfig = new GuildConfig(guildId);
            
            if (!createNewFile)
                return;
            
            File.WriteAllText(GetGcPath(guildId), JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        /// Add a new Ad to the Adlist and save it to disk
        /// </summary>
        /// <param name="ad">The new Ad to be added. Will ignore if already existing</param>
        public void AddAd(Ad ad)
        {
            if (!AdList.Any())
                LoadAdList();
            
            if (AdList.Any(listAd => listAd.Name == ad.Name))
                return;
            
            AdList.Add(ad);
            File.WriteAllText(GetAdPath(ad.Name), JsonConvert.SerializeObject(ad));
        }

        /// <summary>
        /// Remove an Ad from the current Adlist and delete the file
        /// </summary>
        /// <param name="ad">The ad to be deleted. Will ignore if not existing</param>
        public void RemoveAd(Ad ad)
        {
            if (!AdList.Any())
                LoadAdList();
            
            if (AdList.All(listAd => listAd.Name != ad.Name))
                return;

            AdList.Remove(ad);
            File.Delete(GetAdPath(ad.Name));
        }
        
        /// <summary>
        /// Save Changes to files
        /// </summary>
        public void SaveChanges()
            => File.WriteAllText(GetGcPath(GuildConfig.Id), JsonConvert.SerializeObject(GuildConfig, Formatting.Indented));
        
        /// <summary>
        /// Override the Current GuildConfig with the Database files
        /// </summary>
        public void ReloadConfig()
            => GuildConfig = JsonConvert.DeserializeObject<GuildConfig>(File.ReadAllText(GetGcPath(GuildConfig.Id)));
        
        /// <summary>
        /// Reset the current guild to factory settings
        /// </summary>
        public void Reset()
        {
            GuildConfig = new GuildConfig(GuildConfig.Id);
            SaveChanges();
        }

        public void Dispose()
        {
            SaveChanges();
            GuildConfig = null;
        }
    }
}