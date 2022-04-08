using Android.App;
using Android.Content;
using Android.Provider;
using Android.Util;
using Android.Widget;
using SmsHelper.Database;
using SmsHelper.EntityModel;
using SmsHelper.Models;
using System;
using System.Net.Http;

namespace SmsHelper
{
    [BroadcastReceiver(Exported = false)]
    //[IntentFilter(new[] { Telephony.Sms.Intents.SmsReceivedAction }, Priority = (int)IntentFilterPriority.HighPriority)]
    public class SmsReceiver : BroadcastReceiver
    {
        protected string message, address = string.Empty;
        protected string _tag = "SmsReceiver";
        public override async void OnReceive(Context context, Intent intent)
        {
            if (intent.Action.Equals(Telephony.Sms.Intents.SmsReceivedAction))
            {
                var date = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
                var msgs = Telephony.Sms.Intents.GetMessagesFromIntent(intent);
                string phoneNumber = MainActivity.PhoneNumber;
                var message = "";
                var address = "";
                var messageId = 0;
                foreach (var sms in msgs)
                {
                    address = sms.OriginatingAddress;
                    message += sms.MessageBody;
                    messageId = sms.IndexOnSim;
                    
                    Log.Debug(_tag, $"{sms.MessageBody}");
                }

                Log.Debug(_tag, $"from {address} to {phoneNumber} : {message}\n id:{messageId} date:{date}");
                Toast.MakeText(Application.Context, $"from {address} to {phoneNumber} : {message}", ToastLength.Short).Show();

                using var client = new HttpClient();
                var body = new SmsPostData(message, address, phoneNumber);
                var result = await client.PostAsJsonAsync(MainActivity.ApiUrl, body);
                var resultStr = await result.Content.ReadAsStringAsync();
                Log.Debug(_tag, resultStr);
                if (resultStr != null)
                {
                    using var dbContext = new DbContext();
                    dbContext.SmsRecords.Add(
                        new SmsRecord
                        {
                            Content = message,
                            Sender = address,
                            Date = date,
                            Result = resultStr,
                            ReceiveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        }
                    );
                    dbContext.SaveChanges();
                    Toast.MakeText(Application.Context, resultStr, ToastLength.Long).Show();
                }
            }
        }
    }
}