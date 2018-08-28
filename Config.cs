namespace TeamSpeak3ServerQueryProxy
{
    using System;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.IO;

    public partial class Config
    {
        [JsonProperty("server_id")] public long ServerId { get; set; }

        [JsonProperty("remote")] public Listen Remote { get; set; }

        [JsonProperty("listen")] public Listen Listen { get; set; }

        [JsonProperty("packet_changer")] public PacketChangerConfig PacketChangerConfig { get; set; }

        [JsonProperty("white_list")] public string[] WhiteListConfig { get; set; }

        [JsonProperty("update_at")] public long UpdateAt { get; set; }
    }

    public partial class Listen
    {
        [JsonProperty("ip")] public string Ip { get; set; }

        [JsonProperty("port")] public long Port { get; set; }

        [JsonProperty("file_transfer")] public long FileTransfer { get; set; }
    }

    public partial class PacketChangerConfig
    {
        [JsonProperty("auth_replacement")] public AuthReplacement[] AuthReplacement { get; set; }
        [JsonProperty("file_transfer_port")] public bool FileTransferPort { get; set; }
    }

    public partial class AuthReplacement
    {
        [JsonProperty("login_replace")] public string LoginReplace { get; set; }

        [JsonProperty("password_replace")] public string PasswordReplace { get; set; }

        [JsonProperty("to_login")] public string ToLogin { get; set; }

        [JsonProperty("to_password")] public string ToPassword { get; set; }
    }

    public partial class Config
    {
        public static Config[] FromJson(string json) =>
            JsonConvert.DeserializeObject<Config[]>(json, TeamSpeak3ServerQueryProxy.Converter.Settings);

        public static Config[] LoadConfigFromFile(string fileName = "config.json")
        {
            var fstream = File.OpenRead(Environment.CurrentDirectory + "/" + fileName);
            var array = new byte[fstream.Length];
            fstream.Read(array, 0, array.Length);
            var configString = System.Text.Encoding.Default.GetString(array);
            return FromJson(configString);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this Config[] self) =>
            JsonConvert.SerializeObject(self, TeamSpeak3ServerQueryProxy.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
            },
        };
    }
}