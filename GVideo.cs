using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
namespace ytvidlandr
{
    public class GVideo
    {
        [JsonProperty(PropertyName ="id")]
        public string ID { get; set; }
        [JsonProperty(PropertyName ="title")]
        public string Title { get; set; }
        [JsonProperty(PropertyName ="description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName ="agerestricted")]
        public bool AgeRestricted { get; set; }
        [JsonProperty(PropertyName ="thumbnailURL")]
        public string ThumbnailURL { get; set; }
        [JsonProperty(PropertyName ="author")]
        public GAuthor Author { get; set; }
        [JsonProperty(PropertyName ="uploadDate")]
        public DateTime UploadDate { get; set; }
        [JsonProperty(PropertyName ="length")]
        public long Length { get; set; }
        [JsonProperty(PropertyName ="unlisted")]
        public bool Unlisted { get; set; }
        [JsonProperty(PropertyName ="views")]
        public long Views { get; set; }
        public int ITag { get; set; } = 18;
        public string OriginalLink { get; set; }
        public string Extension { get; set; } = ".mp4";
        public GFormat[] CacheFormats { get; set; } = Array.Empty<GFormat>();
        public string SelectedQuality { get => CacheFormats.Length == 0 ? "(default 360p)" : CacheFormats.First(x => x.ITag == ITag).ToString(); }
    }
}