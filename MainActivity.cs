using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Widget;
using System.Net;
using System.Text.RegularExpressions;
using Xamarin.Essentials;
using System.IO;
using Android;
using AndroidX.Core.App;
using Android.Content;
using System.Web;
using System.Linq;
using AndroidX.RecyclerView.Widget;
using System.Collections.Generic;
using System.Drawing;
using Android.Graphics;
using Bitmap = Android.Graphics.Bitmap;
using Path = System.IO.Path;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Android.Text;
using System.Net.Http.Headers;
using AndroidX.Core.Text;

namespace ytvidlandr
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        List<GVideo> videos;
        EditText urltext;
        RecyclerView listview;
        Button getvideobt, downloadvideobtn;
        string defaultpath = "/storage/emulated/0/Download/";
        public const string BASE_ADDRESS = "https://ytdownloader-wrapper.herokuapp.com";
        public static bool ISDOWNLOADING = false;
        const string prefdialogid = "SeenSlowDownloadAlert";
        const string channelid = "fuck yourself";
        WebClient main_client;
        NotificationManager nService;
        bool HasAccessToInternet;
        bool Wifi;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            nService = (NotificationManager)GetSystemService(NotificationService);
            videos = new List<GVideo>(10);
            HasAccessToInternet = Connectivity.NetworkAccess == (Xamarin.Essentials.NetworkAccess.ConstrainedInternet | Xamarin.Essentials.NetworkAccess.Internet);
            Wifi = !Connectivity.ConnectionProfiles.Contains(ConnectionProfile.Cellular);
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            listview = FindViewById<RecyclerView>(Resource.Id.lv);
            listview.SetLayoutManager(new LinearLayoutManager(this));
            listview.SetAdapter(new YTVAdapter(this, videos));
            urltext = FindViewById<EditText>(Resource.Id.urltext);
            getvideobt = FindViewById<Button>(Resource.Id.buttongetvideo);
            getvideobt.Click += GetVideoBT;
            downloadvideobtn = FindViewById<Button>(Resource.Id.buttondlvideo);
            downloadvideobtn.Click += DownloadVideoBT;
        }

        void UpdVal()
        {
            HasAccessToInternet = Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet;
            Wifi = !Connectivity.ConnectionProfiles.Contains(ConnectionProfile.Cellular);
        }

        void DownloadVideoBT(object sender, EventArgs e)
        {
            View view = sender as View;
            if (videos.Count == 0)
            {
                SMake(view, "Nothing to download");
                return;
            }
            if (CheckSelfPermission(Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Denied)
            {
                RequestPermissions(new string[] { Manifest.Permission.WriteExternalStorage }, 0);
                return;
            }
            AlertDialog.Builder a = new AlertDialog.Builder(this);
            a.SetTitle("Download video");
            a.SetPositiveButton("Yes", (s, a) => DV1(view));
            a.SetNegativeButton("No", (s, a) => { ISDOWNLOADING = false; UpdateDownloadStatus(); });
            a.SetMessage($"{videos.Count} videos will be saved to\n{defaultpath}");
            a.Create().Show();
            UpdateDownloadStatus();
        }
        public void NotifyCollection()
        {
            if (videos.Count == 0)
                UpdateDownloadStatus();
        }
        void DV1(View view)
        {
            Console.WriteLine("DV1");
            UpdVal();
            if (!HasAccessToInternet)
            {
                SMake(view, "No access to Internet");
                return;
            }
            if (!Wifi)
            {
                AlertDialog.Builder zb = new AlertDialog.Builder(this);
                zb.SetTitle("Mobile data")
                    .SetMessage("You're using mobile data, wanna continue?")
                    .SetPositiveButton("Yes", (_, __) => DV2(view))
                    .SetNegativeButton("No", (_, __) => { ISDOWNLOADING = false; UpdateDownloadStatus(); }).Show();
            }
            DV2(view);

        }
        async void DV2(View view)
        {
            Console.WriteLine("DV2");
            string path = null;
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    var ch = new NotificationChannel(channelid, "Download progress", NotificationImportance.Default);
                    nService.CreateNotificationChannel(ch);
                }
                string downloadurl;
                int inprogress = 0,
                    finished = 1;
                Size dlSize = new Size();
                foreach (GVideo v in videos)
                {
                    Console.WriteLine("Being processing video: {0}", v.Title); // todo queue
                    downloadurl = BASE_ADDRESS + $"/format?url={v.OriginalLink}&itag={v.ITag}";
                    path = Path.Combine(defaultpath, v.Title + v.Extension);
                    if (File.Exists(path))
                    {
                        RunOnUiThread(() =>
                        {
                            var feb = new AlertDialog.Builder(this)
                               .SetTitle("File exists")
                               .SetMessage($"File \"{Path.GetFileName(path)}\" is already exists, skip?");
                            feb.SetPositiveButton("Yes", (IDialogInterfaceOnClickListener)null)
                                .SetNegativeButton("No", async (_, __) => { File.Delete(path); await DV3(view, v, downloadurl, path, dlSize, inprogress, finished); });
                            feb.Show();
                        });
                    }
                    else await DV3(view, v, downloadurl, path, dlSize, inprogress, finished);
                }
                int z = videos.Count;
                videos.Clear();
                NotifyCollection();
                listview.GetAdapter().NotifyItemRangeRemoved(0, z);
                UpdateDownloadStatus();
            }
            catch (Exception e)
            {
                File.Delete(path);
                new AlertDialog.Builder(this)
                .SetTitle(e.GetType().FullName)
                .SetMessage(e.Message)
                .Show();
            }
            finally
            {
                ISDOWNLOADING = false;
                UpdateDownloadStatus();
            }
        }
        async Task<bool> DV3(View view, GVideo v, string downloadurl, string path, Size dlSize, int inprogress, int finished)
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                Console.WriteLine("DV3");
                NotificationCompat.Builder b = new NotificationCompat.Builder(this, channelid);
                b.SetContentTitle("Download progress")
                    .SetContentText("Initializing")
                    .SetCategory(Notification.CategoryProgress)
                    .SetSmallIcon(Resource.Drawable.aaaaas);
                Notification n = b.Build();
                nService.Notify(0, n);
                ISDOWNLOADING = true;
                TGState(false);
                UpdateDownloadStatus();
                using (HttpClient hc = new HttpClient())
                {
                    using (var res = await hc.GetAsync(downloadurl))
                    {
                        if (!res.IsSuccessStatusCode)
                        {
                            string ret = await res.Content.ReadAsStringAsync();
                            ISpanned formatted = new SpannedString(ret);
                            if (res.Content.Headers.ContentType.MediaType.Contains("html"))
                                formatted = HtmlCompat.FromHtml(ret, HtmlCompat.FromHtmlModeLegacy);
                            new AlertDialog.Builder(this)
                                .SetTitle("Server error")
                                .SetMessage(formatted)
                                .SetPositiveButton("Close", (IDialogInterfaceOnClickListener)null).Show();
                            throw new ArgumentException();
                        }
                    }
                }
                int index = videos.IndexOf(v) + 1;
                int length = videos.Count;
                using (main_client = new WebClient())
                {
                    main_client.DownloadProgressChanged += (s, a) =>
                    {
                        dlSize = new Size(a.BytesReceived);
                        string prtxt = $"Downloading {index}/{length}{(a.TotalBytesToReceive > 0 ? $" {a.ProgressPercentage}%" : " " + dlSize.Auto())}";
                        b.SetProgress(100, a.ProgressPercentage, a.TotalBytesToReceive < 0)
                        .SetContentText(prtxt)
                        .SetOngoing(true);
                        n = b.Build();
                        nService.Notify(inprogress, n);
                    };
                    main_client.DownloadFileCompleted += (s, a) =>
                    {
                        nService.CancelAll();
                        if (a.Cancelled)
                            SMake(view, "Cancelled");
                        else
                            Snackbar.Make(view, $"Saved to {path}", Snackbar.LengthLong).Show();
                        b = new NotificationCompat.Builder(this, channelid);
                        b.SetContentTitle("Download progress")
                        .SetPriority((int)NotificationImportance.Default)
                        .SetContentText(a.Cancelled ? "Cancelled" : $"Downloaded {length} videos")
                        .SetOngoing(false)
                        .SetCategory(Notification.CategorySystem)
                        .SetSmallIcon(Resource.Drawable.aaaaas);
                        nService.Notify(finished, b.Build());
                    };
                    if (v.CacheFormats.Length > 0 && v.ITag != 18)
                    {
                        var p = GetSharedPreferences("YouTube Video Downloader", FileCreationMode.Private);
                        if (!p.GetBoolean(prefdialogid, false))
                        {
                            p.Edit().PutBoolean(prefdialogid, true).Commit();
                            new AlertDialog.Builder(this)
                                .SetTitle("Slow download")
                                .SetMessage("Non default quality media can be downloaded very slow, because of youtube's caching or something like that.")
                                .SetPositiveButton("OK", (IDialogInterfaceOnClickListener)null)
                                .Show();
                        }
                    }
                    Toast.MakeText(this, "Progress will be displayed in notification bar", ToastLength.Long).Show();
                    await main_client.DownloadFileTaskAsync(downloadurl, path);
                }
            }
            catch (ArgumentException) { }
            catch (WebException) { }
            finally
            {
                ISDOWNLOADING = false;
                UpdateDownloadStatus();
                TGState(true);
            }
            return await tcs.Task;
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
        void SMake(View v, string text)
        {
            Snackbar.Make(v, text, Snackbar.LengthLong).SetAction("Close", s =>
            {
                s.Dispose();
            }
            ).Show();
        }
        bool IsYoutubeLink(string s, out string id)
        {
            Match m = Regex.Match(s, @"http(?:s?):\/\/(?:www\.)?youtu(?:be\.com\/watch\?v=|\.be\/)([\w\-\\_]*)(&(amp;)?‌​[\w\?‌​=]*)?");
            id = m.Groups[1].Value;
            return m.Success;
        }
        async void GetVideoBT(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;
            string url = urltext.Text.Trim();
            if (url.Length == 0)
            {
                SMake(view, "URL field is empty");
                return;
            }
            if (!IsYoutubeLink(url, out _))
            {
                SMake(view, "Not a youtube link");
                return;
            }
            UpdVal();
            if (!HasAccessToInternet)
            {
                SMake(view, "No access to internet");
                return;
            }
            TGState(false);
            try
            {
                using (HttpClient hc = new HttpClient())
                {
                    hc.BaseAddress = new Uri(BASE_ADDRESS);
                    using (var res = await hc.GetAsync($"/info?url={url}"))
                    {
                        switch (res.StatusCode)
                        {
                            case HttpStatusCode.BadRequest:
                                SMake(view, "Video not found.");
                                break;
                            case HttpStatusCode.LengthRequired:
                                SMake(view, "Video is age restricted.");
                                break;
                            case HttpStatusCode.Gone:
                                SMake(view, "Video is private.");
                                break;
                            case HttpStatusCode.InternalServerError:
                                AlertDialog.Builder b = new AlertDialog.Builder(this);
                                b.SetMessage(await res.Content.ReadAsStringAsync())
                                    .SetTitle("Error")
                                    .SetNeutralButton("Close", (IDialogInterfaceOnClickListener)null);
                                b.Show();
                                break;
                            default:
                                string json = await res.Content.ReadAsStringAsync();
                                GVideo g = JsonConvert.DeserializeObject<GVideo>(json);
                                g.OriginalLink = url;
                                videos.Add(g);
                                listview.GetAdapter().NotifyItemInserted(videos.Count - 1);
                                urltext.Text = null;
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AlertDialog.Builder b = new AlertDialog.Builder(this);
                b.SetTitle(e.GetType().FullName);
                b.SetMessage(e.Message);
                b.Create().Show();
                return;
            }
            finally
            {
                UpdateDownloadStatus();
                TGState(true);
            }
        }

        void UpdateDownloadStatus()
            => downloadvideobtn.Enabled = videos.Any() && !ISDOWNLOADING;
        void TGState(bool state)
            => urltext.Enabled = getvideobt.Enabled = state;
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
