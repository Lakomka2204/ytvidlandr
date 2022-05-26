using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Snackbar;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AndroidX.AppCompat.App;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using static Android.Resource;
using AppCompatTextView = AndroidX.AppCompat.Widget.AppCompatTextView;
namespace ytvidlandr
{
    public class YTVAdapter : RecyclerView.Adapter
    {
        List<GVideo> bullshit;
        Context c;
        public YTVAdapter(Context c, List<GVideo> bullshit)
        {
            this.c = c;
            this.bullshit = bullshit;
        }

        public override int ItemCount => bullshit.Count;

        public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            YTVHolder h = holder as YTVHolder;
            GVideo current = bullshit[position];
            using (HttpClient hc = new HttpClient())
            {
                using (var res = await hc.GetAsync(current.ThumbnailURL))
                {
                    using (Stream s = await res.Content.ReadAsStreamAsync())
                    {
                        Bitmap bm = await BitmapFactory.DecodeStreamAsync(s);
                        int width = Resources.System.DisplayMetrics.WidthPixels;
                        int maxWidth = Math.Min(width, bm.Width);
                        int maxHeight = bm.Height;
                        decimal rnd = Math.Min(maxWidth / (decimal)bm.Width, maxHeight / (decimal)bm.Height);
                        int destWidth = (int)Math.Round(bm.Width * rnd);
                        int destHeight = (int)Math.Round(bm.Height * rnd);
                        Bitmap rbm = Bitmap.CreateScaledBitmap(bm, destWidth, destHeight, false);
                        h.Thumbnail.SetImageBitmap(rbm);
                    }
                }
            }
            h.VName.Text = current.Title;
            h.Layout.Click += DisplayInfoClick;
            h.Info = current;
        }

        AndroidX.AppCompat.App.AlertDialog dl;
        private void DisplayInfoClick(object sender, EventArgs e)
        {
            if (dl != null && dl.IsShowing) return;
            try
            {

                LinearLayout l = sender as LinearLayout;
                int pos = -1;
                for (int i = 0; i < l.ChildCount; i++)
                {
                    var c = l.GetChildAt(i);
                    if (c.GetType().Name.Contains("TextView"))
                    {
                        AppCompatTextView tv = c as AppCompatTextView;
                        pos = bullshit.IndexOf(bullshit.First(x => x.Title == tv.Text));
                    }
                }
                Console.WriteLine($"{pos} : {bullshit[pos].Title}");
                GVideo current = bullshit[pos];
                AndroidX.AppCompat.App.AlertDialog.Builder b = new AndroidX.AppCompat.App.AlertDialog.Builder(c);
                TimeSpan ts = TimeSpan.FromSeconds(current.Length);
                
                string msg = 
                    $"Author: {current.Author}\n" +
                    $"Length: {ts}{(ts.TotalHours > 1 ? "\n(woah, that's a lot, r u sure about downloading that👀?)" : "")}\n"+
                $"Selected quality: {current.SelectedQuality}\n";
                dl = b.SetTitle(current.Title)
                .SetMessage(msg)
                .SetNegativeButton("Remove", (s, aa) =>
                {
                    if (MainActivity.ISDOWNLOADING)
                    {
                        Toast.MakeText(c, "Cannot modify list while downloading", ToastLength.Long).Show();
                        return;
                    }
                    if (pos >= 0 && pos < bullshit.Count)
                        bullshit.RemoveAt(pos);
                    MainActivity ma = c as MainActivity;
                    ma.NotifyCollection();
                    NotifyItemRemoved(pos);
                    dl = null;
                })
                .SetNeutralButton("Select quality", async (s, aa) => await DisplayQuality(current))
                .SetPositiveButton("Full info", (s, aa) => DisplayFullInfo(current))
                .Create();
                dl.Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(c, ex.Message, ToastLength.Long).Show();
            }
        }
        void DisplayFullInfo(GVideo current)
        {
            string msg = $"ID: {current.ID}\n\n" +
                $"Title: {current.Title}\n\n" +
                $"Description: {current.Description}\n\n" +
                $"Age restricted: {(current.AgeRestricted? "Yes" : "No")}\n\n" +
                $"Unlisted: {(current.Unlisted ? "Yes" : "No")}\n\n" +
                $"Upload date: {current.UploadDate}\n\n" +
                $"Length: {TimeSpan.FromSeconds(current.Length)}\n\n" +
                $"Link: {current.OriginalLink}\n\n" +
                $"Selected quality: {current.SelectedQuality}\n\n" +
                $"Views: {current.Views}\n\n" +
                $"Thumbnail URL: {current.ThumbnailURL}\n\n" +
                $"File name: {current.Title+current.Extension}";
            AlertDialog.Builder b = new AlertDialog.Builder(c);
            b.SetTitle("Full info")
                .SetMessage(msg)
                .SetNegativeButton("Open download URL", (s, aa) =>
                {
                    var uri = Android.Net.Uri.Parse($"{MainActivity.BASE_ADDRESS}/format?url=https://youtu.be/{current.ID}&itag={current.ITag}");
                    var intent = new Intent(Intent.ActionView, uri);
                    c.StartActivity(intent);
                })
                .SetPositiveButton("Close", (IDialogInterfaceOnClickListener)null);
            b.Create().Show();
        }
        async Task DisplayQuality(GVideo current)
        {
            if (MainActivity.ISDOWNLOADING)
            {
                Toast.MakeText(c, "Cannot change quality while downloading", ToastLength.Long).Show();
                return;
            }
            AlertDialog.Builder b = new AlertDialog.Builder(c);
            AlertDialog a = null;
            b.SetTitle("Please wait");
            b.SetMessage("Getting available streams...");
            b.SetCancelable(false);
            a = b.Create();
            a.Show();
            using (HttpClient hc = new HttpClient()
            { BaseAddress = new Uri(MainActivity.BASE_ADDRESS) })
            {
                if (current == null)
                {
                    Toast.MakeText(c, "Current obj is null!", ToastLength.Short).Show();
                    return;
                }
                if (!current.CacheFormats.Any())
                {

                    using (var res = await hc.GetAsync($"/formats?url={current.OriginalLink}"))
                    {
                        if (!res.IsSuccessStatusCode)
                        {
                            Toast.MakeText(c, "Error, try again.", ToastLength.Short).Show();
                            return;
                        }
                        current.CacheFormats = JsonConvert.DeserializeObject<GFormat[]>(await res.Content.ReadAsStringAsync());
                    }
                }
                string[] quality = current.CacheFormats.Select(x => x.ToString()).ToArray();
                b.SetTitle("Select quality")
                    .SetMessage((string)null)
                    .SetCancelable(true)
                    .SetItems(quality, (s, a) =>
                {
                    GFormat selectedformat = current.CacheFormats[a.Which];
                    current.ITag = selectedformat.ITag;
                    current.Extension = '.'+selectedformat.Mime.Split('/')[1].Split(';')[0];
                    Toast.MakeText(c, $"Selected quality: {quality[a.Which]}", ToastLength.Short).Show();
                });
                a.Dismiss();
                a = b.Create();
                a.Show();

            }
        }
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemview = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.recycler_element, parent, false);
            YTVHolder h = new YTVHolder(itemview);
            return h;
        }
        public override void OnDetachedFromRecyclerView(RecyclerView recyclerView)
        {
            base.OnDetachedFromRecyclerView(recyclerView);
        }
    }
    public class YTVHolder : RecyclerView.ViewHolder
    {
        public TextView VName { get; set; }
        public ImageView Thumbnail { get; set; }
        public LinearLayout Layout { get; set; }
        public GVideo Info { get; set; }
        public YTVHolder(View itemView) : base(itemView)
        {
            VName = itemView.FindViewById<TextView>(Resource.Id.textView);
            Thumbnail = itemView.FindViewById<ImageView>(Resource.Id.imageView);
            Layout = itemView.FindViewById<LinearLayout>(Resource.Id.recycler_mlayout);
        }
    }
}