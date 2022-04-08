using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace SmsHelper
{
    [Service(Exported = false)]
    class BgService : Service
    {
        private static BgService instence = null;

        public const int FORSERVICE_NOTIFICATION_ID = 9487;
        public const string MAIN_ACTIVITY_ACTION = "Main_activity";
        public const string PUT_EXTRA = "has_service_been_started";
        public const string CHANNEL_ID = "bgService";

        public static bool IsRunning { get; private set; } = false;
        public static bool IsConnect { get; private set; } = false;

        enum MessageType
        {
            connect = 200,
            alive = 201
        }


        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        private PendingIntent BuildIntentToShowMainActivity()
        {
            var notificationIntent = new Intent(this, typeof(MainActivity));
            notificationIntent.SetAction(MAIN_ACTIVITY_ACTION);
            notificationIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
            notificationIntent.PutExtra(PUT_EXTRA, true);

            var pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, PendingIntentFlags.UpdateCurrent);
            return pendingIntent;
        }

        /*
         * This service will run until stopped explicitly because we are returning sticky
         */
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (instence == null)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    NotificationChannel channel = new NotificationChannel(CHANNEL_ID, "Channel", NotificationImportance.Default)
                    {
                        Description = "Foreground Service Channel"
                    };

                    NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
                    notificationManager.CreateNotificationChannel(channel);


                    Notification notification = new Notification.Builder(this, CHANNEL_ID)
                        .SetContentTitle("运行中")
                        .SetContentText("请确认省电模式为关闭")
                        .SetSmallIcon(Resource.Drawable.icon)
                        .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon))
                        .SetContentIntent(BuildIntentToShowMainActivity())
                        .SetOngoing(true)
                        .Build();
                    StartForeground(FORSERVICE_NOTIFICATION_ID, notification);
                }
                else
                {
                    Notification notification = new Notification.Builder(this)
                     .SetContentTitle("运行中")
                     .SetContentText("请确认省电模式为关闭")
                     .SetSmallIcon(Resource.Drawable.icon)
                     .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon))
                     .SetContentIntent(BuildIntentToShowMainActivity())
                     .SetOngoing(true)
                     .Build();
                    StartForeground(FORSERVICE_NOTIFICATION_ID, notification);
                }
                IsRunning = true;
                SmsReceiver smsReceiver = new SmsReceiver();
                IntentFilter intentFilter = new IntentFilter();
                intentFilter.AddAction(Telephony.Sms.Intents.SmsReceivedAction);
                intentFilter.Priority = (int)IntentFilterPriority.HighPriority;
                RegisterReceiver(smsReceiver, intentFilter);
                Toast.MakeText(this, "服务已启动", ToastLength.Short).Show();
                SocketInit();
                instence = this;
            }
            return StartCommandResult.Sticky;
        }

        private async void SocketInit()
        {
            await Task.Run(SocketConnect);
        }

        private static readonly Timer timer = new Timer(new TimerCallback(SocketReconnect));
        private static readonly WebSocket Ws = new WebSocket(MainActivity.WsUrl);
        //private static readonly WebSocket Ws = new WebSocket("ws://34.92.185.101:8088");

        private static void SocketReconnect(object obj)
        {
            Log.Debug("SocketReconnect", "on retry");
            Ws.ConnectAsync();
        }

        private async void SocketConnect()
        {
            //using var Ws = new WebSocket("wss://pay-vn.maxpay98.com/");
            Ws.OnMessage += Ws_OnMessage;
            Ws.OnClose += Ws_OnClose;
            Ws.OnOpen += Ws_OnOpen;
            //Ws.WaitTime = TimeSpan.FromSeconds(10);
            Ws.ConnectAsync();
            while (true)
            {
                await Task.Delay(1000);
                if (Ws.IsAlive)
                {
                    var aliveMessage = new
                    {
                        type = MessageType.alive,
                        phoneNumber = MainActivity.PhoneNumber
                    };
                    if (string.IsNullOrEmpty(MainActivity.PhoneNumber))
                        IsConnect = false;
                    else
                        IsConnect = true;
                    Ws.Send(JsonConvert.SerializeObject(aliveMessage));
                }
                else
                    Ws.Connect();
            }

        }

        private void Ws_OnOpen(object sender, EventArgs e)
        {
            var connectMessage = new
            {
                type = MessageType.connect,
                phoneNumber = MainActivity.PhoneNumber
            };
            Ws.Send(JsonConvert.SerializeObject(connectMessage));
        }

        private void Ws_OnClose(object sender, CloseEventArgs e)
        {
            Log.Debug("socket", e.Reason + e.Code);
            IsConnect = false;
            if (e.Code == (ushort)CloseStatusCode.Away) // You should have an escape from the reconnecting loop.
                return;
            //timer.Change(3000, Timeout.Infinite);

            //var retry = 0;
            //while (retry++ < 5 && !Ws.IsAlive)
            //{
            //    Thread.Sleep(3000);
            //    Log.Debug("retry", retry.ToString());
            //    Ws.Connect();
            //}
            //Ws.Log.Error("The reconnecting has failed.");

        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            //Log.Debug("alive", e.Data);
        }

        public override void OnDestroy()
        {
            _ = Log.Debug("serviceCall", "OnDestroy");
            IsConnect = false;
            IsRunning = false;
            instence = null;
            StopForeground(true);
            base.OnDestroy();
        }

        public override bool StopService(Intent name)
        {
            return base.StopService(name);
        }


    }
}