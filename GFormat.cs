using Newtonsoft.Json;
using System.Linq;

namespace ytvidlandr
{
    public class GFormat
    {
        [JsonProperty(PropertyName = "itag")]
        public int ITag { get; set; }
        [JsonProperty(PropertyName = "mimeType")]
        public string Mime { get; set; }
        [JsonProperty(PropertyName = "bitrate")]
        public ulong Bitrate { get; set; }
        [JsonProperty(PropertyName = "width")]
        public int Width { get; set; }
        [JsonProperty(PropertyName = "height")]
        public int Height { get; set; }
        [JsonProperty(PropertyName = "contentLength")]
        public int ContentLength { get; set; }
        [JsonProperty(PropertyName = "quality")]
        public string Quality { get; set; }
        [JsonProperty(PropertyName = "fps")]
        public int Fps { get; set; }
        [JsonProperty(PropertyName = "qualityLabel")]
        public string QualityLabel { get; set; }
        [JsonProperty(PropertyName = "onlyAudio")]
        public bool OnlyAudio { get; set; }
        [JsonProperty(PropertyName = "onlyVideo")]
        public bool OnlyVideo { get; set; }

        public override string ToString()
        {
            string ret;
            string type = $".{Mime.Split(';')[0].Split('/')[1]}";
            if (OnlyAudio) ret = $"Audio {type}";
            else if (OnlyVideo) ret = $"Video ({QualityLabel}) {type} (no audio)";
            else ret = $"Video ({QualityLabel}) {type} (with audio)";
            return ret;
        }
    }
}