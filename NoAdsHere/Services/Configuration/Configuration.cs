using System;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace NoAdsHere.Services.Configuration
{
    public sealed class Config
    {
        private static readonly Logger Logger = LogManager.GetLogger("Config");

        private Config()
        {
        }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("command_prefix")]
        public string Prefix { get; set; } = "?nah";

        [JsonProperty("command_on_mention")]
        public bool TriggerOnMention { get; set; } = true;
        
        [JsonProperty("shards")]
        public int TotalShards { get; set; } = 1;

        public class ConfigDatabase
        {
            [JsonProperty("host")]
            public string Host { get; set; } = "localhost";

            [JsonProperty("port")]
            public int Port { get; set; } = 27017;

            [JsonProperty("db")]
            public string Db { get; set; } = "admin";

            [JsonProperty("user")]
            public string Username { get; set; } = "user";

            [JsonProperty("password")]
            public string Password { get; set; } = "password";

            [JsonIgnore]
            public string ConnectionString => $"mongodb://{Username}:{Password}@{Host}:{Port}/{Db}";
        }

        [JsonProperty("db")]
        public ConfigDatabase Database { get; set; } = new ConfigDatabase();

        public class ConfigWebHookLogger
        {
            [JsonProperty("id")]
            public ulong Id { get; set; }

            [JsonProperty("token")]
            public string Token { get; set; } = "";
        }

        [JsonProperty("webhook")]
        public ConfigWebHookLogger WebHookLogger { get; set; } = new ConfigWebHookLogger();

        public static Config Load()
        {
            Logger.Info("Loading configuration from config.json");
            if (File.Exists("config.json"))
            {
                var json = File.ReadAllText("config.json");
                return JsonConvert.DeserializeObject<Config>(json);
            }
            var config = new Config();
            config.Save();
            throw new InvalidOperationException("Configuration file created; insert token and restart.");
        }

        [JsonProperty("point_decrease_hours")]
        public double PointDecreaseHours { get; set; } = 12;
        
        [JsonProperty("max_levenshtein_distance")]
        public int MaxLevenshteinDistance { get; set; } = 4;

        public static Config LoadFrom(string path)
        {
            Logger.Info($"Loading configuration from {path}");
            path = Path.Combine(path, "config.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<Config>(json);
            }
            else
                throw new FileNotFoundException("Configuration file needs to be present!");
        }

        public void Save()
        {
            Logger.Info("Saving configuration to config.json");
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText("config.json", json);
        }
    }
}