using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;
using SmsHelper.Database;
using SmsHelper.EntityModel;
using SmsHelper.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Timers;
using Xamarin.Essentials;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using SmsHelper.Components;

namespace SmsHelper
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : AppCompatActivity, Google.Android.Material.Navigation.NavigationBarView.IOnItemSelectedListener
    {

        AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout swipeRefreshLayout1;
        FrameLayout smsFrameLayout;
        LinearLayout settingLinearLayout;

        List<SmsDetail> smsDetails = new List<SmsDetail>();
        SmsArrayAdapter adapter;
        ListView listView;
        static TextView onlineStatus;
        TextView phoneNumberText;
        TextView secretKeyText;
        TextView versionText;
        EditText phoneNumberEditText;
        EditText secretKeyEditText;
        EditText apiUrlEditText;
        EditText wsUrlEditText;
        Button changePhoneButton;
        Google.Android.Material.FloatingActionButton.FloatingActionButton fab;
        static Timer timer = new Timer(1000);

        private readonly string phoneNumberFilePath = Path.Combine
            (System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "phoneNumber.txt");
        private readonly string secretKeyFilePath = Path.Combine
            (System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "secretKey.txt");
        private readonly string apiUrlFilePath = Path.Combine
            (System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "apiUrl.txt");
        private readonly string wsUrlFilePath = Path.Combine
            (System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "wsUrl.txt");
        public static string PhoneNumber { get; set; }
        public static string SecretKey { get; set; } = "";
        public static string ApiUrl { get; set; } = "";
        public static string WsUrl { get; set; } = "";

        private const string defaultApiUrl = "https://*******";
        private const string defaultWsUrl = "wss://*******";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            swipeRefreshLayout1 = FindViewById<AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout1);
            listView = FindViewById<ListView>(Resource.Id.listView);
            smsFrameLayout = FindViewById<FrameLayout>(Resource.Id.smsFrameLayout);
            settingLinearLayout = FindViewById<LinearLayout>(Resource.Id.settingLinearLayout);
            onlineStatus = FindViewById<TextView>(Resource.Id.onlineStatus);
            phoneNumberEditText = FindViewById<EditText>(Resource.Id.phoneNumber);
            secretKeyEditText = FindViewById<EditText>(Resource.Id.secertKey);
            apiUrlEditText = FindViewById<EditText>(Resource.Id.apiUrl);
            wsUrlEditText = FindViewById<EditText>(Resource.Id.wsUrl);
            phoneNumberText = FindViewById<TextView>(Resource.Id.phoneNumberText);
            secretKeyText = FindViewById<TextView>(Resource.Id.secretKeyText);
            versionText = FindViewById<TextView>(Resource.Id.versionText);
            changePhoneButton = FindViewById<Button>(Resource.Id.changePhoneButton);
            fab = FindViewById<Google.Android.Material.FloatingActionButton.FloatingActionButton>(Resource.Id.floatingActionButton1);

            var _version = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;
            versionText.Text = _version;
            changePhoneButton.Click += Button_Clicked;

            fab.Click += (sender, e) =>
            {
                RefreshSms();
            };

            swipeRefreshLayout1.Refresh += (sender, e) =>
            {
                RefreshSms();
                swipeRefreshLayout1.Refreshing = false;
            };

            onlineStatus.SetTextColor(Android.Graphics.Color.Red);
            onlineStatus.Text = "离线";
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);

            navigation.SelectedItemId = Resource.Id.navigation_setting;
            navigation.SetOnItemSelectedListener(this);

            RequestPermissions(new string[] {
                Manifest.Permission.ReadSms,
                Manifest.Permission.ReceiveSms,
                Manifest.Permission.ReadPhoneState,
                Manifest.Permission.ReadPhoneNumbers,
                Manifest.Permission.AccessNetworkState,
                Manifest.Permission.Internet
            }, 0);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_inbox:
                    smsFrameLayout.Visibility = ViewStates.Visible;
                    settingLinearLayout.Visibility = ViewStates.Invisible;
                    return true;
                case Resource.Id.navigation_setting:
                    smsFrameLayout.Visibility = ViewStates.Invisible;
                    settingLinearLayout.Visibility = ViewStates.Visible;
                    return true;
            }
            return false;
        }

        private void SetTimer()
        {
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += OnTimedEvent;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (!IsMyServiceRunning(typeof(BgService)))
            {
                Log.Debug("BgService", "NotAlive");
                Toast.MakeText(this, "服务已重启", ToastLength.Short);
                StartBackgroundService();
            }
            if (BgService.IsConnect)
            {
                onlineStatus.Text = "在线";
                onlineStatus.SetTextColor(Android.Graphics.Color.Green);
                Intent i = new Intent(this, typeof(BgService));
            }
            else
            {
                onlineStatus.Text = "离线";
                onlineStatus.SetTextColor(Android.Graphics.Color.Red);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            InitPhoneNumber();
            InitSecretKey();
            InitSms();
            InitApiUrl();
            InitWsUrl();
            StartBackgroundService();
            SetTimer();
            timer.Start();
        }

        public void InitSms()
        {
            GetAllSms();
            //adapter.AddAll(smsDetails.Select(p => p.Content).ToList());
            adapter = new SmsArrayAdapter(this, Android.Resource.Layout.SimpleListItem1, smsDetails); ;

            adapter.SetNotifyOnChange(true);
            listView.Adapter = adapter;

            listView.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs e)
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                var smsDetail = smsDetails.Find(p => p.Id == e.Position);
                alert.SetTitle("确认送出？");
                alert.SetMessage($"发送方：{smsDetail.Sender} \n内容：{smsDetail.Content}");
                Log.Debug("date", smsDetail.Date);
                alert.SetPositiveButton("送出", async (senderAlert, e) =>
                {

                    using var client = new HttpClient();
                    var body = new SmsPostData(smsDetail.Content, smsDetail.Sender, PhoneNumber);
                    var result = await client.PostAsJsonAsync(ApiUrl, body);
                    var resultStr = await result.Content.ReadAsStringAsync();
                    Log.Debug("send sms", resultStr);
                    Toast.MakeText(Application.Context, resultStr, ToastLength.Long).Show();

                    if (resultStr != null)
                    {
                        using var dbContext = new DbContext();
                        dbContext.SmsRecords.Add(
                                        new SmsRecord
                                        {
                                            SmsId = smsDetail.SmsId,
                                            Content = smsDetail.Content,
                                            Sender = smsDetail.Sender,
                                            Date = smsDetail.Date,
                                            Result = resultStr,
                                            ReceiveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                        }
                        );
                        dbContext.SaveChanges();
                    }
                    RefreshSms();
                });
                alert.SetNegativeButton("取消", (senderAlert, e) => { });
                Dialog dialog = alert.Create();
                dialog.Show();
            };

        }

        public void RefreshSms()
        {
            RunOnUiThread(() =>
            {
                GetAllSms();
                adapter.Clear();
                adapter.AddAll(smsDetails);
                adapter.NotifyDataSetChanged();
            });

        }

        public static void ChangeServiceLabel(string str)
        {
            onlineStatus.Text = str;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            SecretKey = secretKeyEditText.Text;
            secretKeyText.Text = SecretKey;
            File.WriteAllText(secretKeyFilePath, SecretKey);

            PhoneNumber = phoneNumberEditText.Text;
            phoneNumberText.Text = PhoneNumber;
            File.WriteAllText(phoneNumberFilePath, PhoneNumber);

            ApiUrl = apiUrlEditText.Text;
            File.WriteAllText(apiUrlFilePath, ApiUrl);

            WsUrl = wsUrlEditText.Text;
            File.WriteAllText(wsUrlFilePath, WsUrl);

            Toast.MakeText(this, "保存成功", ToastLength.Short).Show();
        }



        /// <summary>
        /// 开启背景服务(check alive)
        /// </summary>
        private void StartBackgroundService()
        {

            var current = Connectivity.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                Toast.MakeText(this, "当前未有网络连线，请连线后再次开启本应用", ToastLength.Long).Show();

                this.Dispose();
                return;
            }

            //return;
            Intent i = new Intent(this, typeof(BgService));

            _ = Build.VERSION.SdkInt >= BuildVersionCodes.O ? StartForegroundService(i) : StartService(i);
        }

        private void InitPhoneNumber()
        {
            if (File.Exists(phoneNumberFilePath))
            {
                Log.Debug("FileCheck", "Exist");
                string number = File.ReadAllText(phoneNumberFilePath);
                Log.Debug("FileCheck", number);
                PhoneNumber = number;
            }
            else
            {
                Log.Debug("FileCheck", "Not Exist");
                try
                {
                    TelephonyManager tMgr = (TelephonyManager)GetSystemService(TelephonyService);
                    PhoneNumber = tMgr.Line1Number.Replace("+", "");
                }
                catch
                {
                    PhoneNumber = "";
                }
            }
            phoneNumberEditText.Text = PhoneNumber;
            phoneNumberText.Text = PhoneNumber;
        }

        private void InitSecretKey()
        {
            if (File.Exists(secretKeyFilePath))
            {
                Log.Debug("FileCheck", "Exist");
                string secret = File.ReadAllText(secretKeyFilePath);
                Log.Debug("FileCheck", secret);
                SecretKey = secret;
            }
            secretKeyText.Text = SecretKey;
            secretKeyEditText.Text = SecretKey;
        }

        private void InitApiUrl()
        {
            if (File.Exists(apiUrlFilePath))
            {
                Log.Debug("FileCheck", "Exist");
                string apiUrl = File.ReadAllText(apiUrlFilePath);
                Log.Debug("FileCheck", apiUrl);
                ApiUrl = apiUrl;
            }
            else { 
                ApiUrl = defaultApiUrl; 
            }
            apiUrlEditText.Text = ApiUrl;
        }

        private void InitWsUrl()
        {
            if (File.Exists(wsUrlFilePath))
            {
                Log.Debug("FileCheck", "Exist");
                string wsUrl = File.ReadAllText(wsUrlFilePath);
                Log.Debug("FileCheck", wsUrl);
                WsUrl = wsUrl;
            }
            else
            {
                WsUrl = defaultWsUrl;
            }
            wsUrlEditText.Text = WsUrl;
        }


        public void GetAllSms()
        {
            using var dbContext = new DbContext();
            string INBOX = "content://sms/inbox";
            string[] reqCols = new string[] { "_id", "thread_id", "address", "person", "date", "body", "type" };
            Android.Net.Uri uri = Android.Net.Uri.Parse(INBOX);
            var cursor = ContentResolver.Query(uri, reqCols, null, null, null);
            var index = 0;
            var dateQuery = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - 60 * 60 * 48;
            if (cursor.MoveToFirst())
            {
                smsDetails.Clear();
                do
                {
                    string messageId = cursor.GetString(cursor.GetColumnIndex(reqCols[0]));
                    string threadId = cursor.GetString(cursor.GetColumnIndex(reqCols[1]));
                    string address = cursor.GetString(cursor.GetColumnIndex(reqCols[2]));
                    string name = cursor.GetString(cursor.GetColumnIndex(reqCols[3]));
                    string date = cursor.GetString(cursor.GetColumnIndex(reqCols[4]))[..^3];
                    string msg = cursor.GetString(cursor.GetColumnIndex(reqCols[5]));
                    string type = cursor.GetString(cursor.GetColumnIndex(reqCols[6]));

                    if (Convert.ToInt32(date) * 1 < dateQuery) continue;

                    var smsRecordDetail = dbContext.SmsRecords.Where(p => p.Content == msg).OrderByDescending(r => r.ReceiveTime).FirstOrDefault();
                    if (smsRecordDetail is object)
                    {
                        smsDetails.Add(new SmsDetail()
                        {
                            Result = smsRecordDetail.Result,
                            ReceiveTime = smsRecordDetail.ReceiveTime,
                            Sender = address,
                            Content = msg,
                            SmsId = Convert.ToInt32(messageId),
                            Date = date,
                            Id = index++,
                            IsSended = true
                        });
                    }
                    else
                    {
                        smsDetails.Add(new SmsDetail()
                        {
                            Result = $"未发送",
                            ReceiveTime = "",
                            Sender = address,
                            Content = msg,
                            SmsId = Convert.ToInt32(messageId),
                            Date = date,
                            Id = index++,
                            IsSended = false
                        });
                    }
                } while (cursor.MoveToNext());
            }
        }

        private bool IsMyServiceRunning(Type _class)
        {
            ActivityManager manager = (ActivityManager)GetSystemService(ActivityService);
            foreach (var service in manager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.Equals(Java.Lang.Class.FromType(_class).CanonicalName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

