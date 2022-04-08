using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace SmsHelper.Models
{
    class SmsPostData
    {
        public SmsPostData(string _content, string _sender, string _receiver)
        {
            var _version = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;
            content = _content;
            sender = _sender;
            receiver = _receiver;
            receive_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            version = _version;

            var key = MainActivity.SecretKey;
            using MD5 md5 = MD5.Create();
            var signStr = $"content={content}&receive_time={receive_time}&receiver={receiver}&sender={sender}&version={version}&key={key}";
            var bytes = Encoding.UTF8.GetBytes(signStr);
            var hash = md5.ComputeHash(bytes);
            var _sign = BitConverter.ToString(hash)
              .Replace("-", string.Empty)
              .ToLower();

            sign = _sign;
        }
        public string content { get; set; }
        public string sender { get; set; }
        public string receiver { get; set; }
        public string receive_time { get; set; }
        public string version { get; set; }
        public string sign { get; set; }
    }
}