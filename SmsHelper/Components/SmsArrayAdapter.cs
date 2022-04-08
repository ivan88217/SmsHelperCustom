using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Text;
using SmsHelper.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmsHelper.Components
{
    class SmsArrayAdapter : ArrayAdapter<SmsDetail>
    {
        private List<SmsDetail> smsDetails;
        Context context;
        public SmsArrayAdapter(Context context, int textViewResourceId, List<SmsDetail> obj) : base(context, textViewResourceId, obj)
        {
            this.context = context;
            smsDetails = obj;
            SetNotifyOnChange(true);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = LayoutInflater.From(context).Inflate(Resource.Layout.sms_listview, parent, false);
            var alertView = view.FindViewById<TextView>(Resource.Id.alert);
            var senderView = view.FindViewById<TextView>(Resource.Id.sender);
            var timeView = view.FindViewById<TextView>(Resource.Id.time);
            var receiveTimeView = view.FindViewById<TextView>(Resource.Id.receiveTime);
            var contentView = view.FindViewById<TextView>(Resource.Id.content);
            var smsDetail = smsDetails[position];
            view.SetBackgroundColor(Android.Graphics.Color.Rgb(220,220,220));
            if (!smsDetail.IsSended)
            {
                view.SetBackgroundColor(Android.Graphics.Color.White);
                alertView.Text = "未发送";
                alertView.Visibility = ViewStates.Visible;
            }
            if (smsDetail.IsSended && smsDetail.Result != "success")
            {
                alertView.Text = smsDetail.Result;
                alertView.Visibility = ViewStates.Visible;
            }
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(int.Parse(smsDetail.Date)).AddHours(8);
            senderView.Text = "来自 : " + smsDetail.Sender;
            timeView.Text = "收到时间 : " + dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            receiveTimeView.Text = "回调时间 : " + smsDetail.ReceiveTime;
            contentView.Text = "内容 : " + smsDetail.Content;
            return view;
        }

        public override void AddAll(ICollection collection)
        {
            base.AddAll(collection);
            NotifyDataSetChanged();
        }
    }
}