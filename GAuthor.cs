using Newtonsoft.Json;

namespace ytvidlandr
{
    public class GAuthor
    {
        [JsonProperty(PropertyName ="id")]
        public string ID { get; set; }
        [JsonProperty(PropertyName ="name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName ="subCount")]
        public long SubCount { get; set; }
        [JsonProperty(PropertyName ="isVerified")]
        public bool IsVerified { get; set; }
        public override string ToString() => Name;
    }
}