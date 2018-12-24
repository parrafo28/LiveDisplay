﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using LiveDisplay.Adapters;
using LiveDisplay.Servicios.Notificaciones;

namespace LiveDisplay.Servicios.FloatingNotification
{
    //Will spawn a View to show the clicked notification in the list, while the music is playing.
    [Service(Enabled =true)]
    class FloatingNotification : Service
    {
        private IWindowManager windowManager;
        private LinearLayout floatingNotificationView;
        private TextView floatingNotificationTitle;
        private TextView floatingNotificationText;
        private TextView floatingNotificationAppName;
        private TextView floatingNotificationWhen;
        private int position; //Represents the notification position in the NotificationList.

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }
        public override void OnCreate()
        {
            
            base.OnCreate();
                windowManager = (IWindowManager)GetSystemService(WindowService).JavaCast<IWindowManager>();

            var lol = LayoutInflater.From(this);

            floatingNotificationView = (LinearLayout)lol.Inflate(Resource.Layout.FloatingNotification, null);

            int width = 200;
            var floatingNotificationWidth= TypedValue.ApplyDimension(ComplexUnitType.Dip, width, Resources.DisplayMetrics);

            WindowManagerLayoutParams layoutParams = new WindowManagerLayoutParams();
            layoutParams.Width = (int)floatingNotificationWidth;
            layoutParams.Height = ViewGroup.LayoutParams.WrapContent;
            layoutParams.Type = WindowManagerTypes.Phone;
            layoutParams.Flags = WindowManagerFlags.NotFocusable;
            layoutParams.Format = Android.Graphics.Format.Translucent;
            layoutParams.Gravity = GravityFlags.CenterHorizontal | GravityFlags.CenterVertical;

            windowManager.AddView(floatingNotificationView, layoutParams);
            floatingNotificationAppName= floatingNotificationView.FindViewById<TextView>(Resource.Id.floatingappname);
            floatingNotificationWhen = floatingNotificationView.FindViewById<TextView>(Resource.Id.floatingwhen);
            floatingNotificationTitle = floatingNotificationView.FindViewById<TextView>(Resource.Id.floatingtitle);
            floatingNotificationText = floatingNotificationView.FindViewById<TextView>(Resource.Id.floatingtext);

            NotificationAdapterViewHolder.ItemClicked += NotificationAdapterViewHolder_ItemClicked;
            NotificationAdapterViewHolder.ItemLongClicked += NotificationAdapterViewHolder_ItemLongClicked;

            try
            {
                floatingNotificationView.Click += FloatingNotificationView_Click;
            }
            catch(Exception e)
            {
                Log.Error("LiveDisplay", e.Message);
            }
        }


        private void NotificationAdapterViewHolder_ItemLongClicked(object sender, Notificaciones.NotificationEventArgs.NotificationItemClickedEventArgs e)
        {
            if (OpenNotification.IsRemovable(e.Position))
            {
                using (NotificationSlave notificationSlave = NotificationSlave.NotificationSlaveInstance())
                {
                    if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
                    {
                        int notiId = CatcherHelper.statusBarNotifications[position].Id;
                        string notiTag = CatcherHelper.statusBarNotifications[position].Tag;
                        string notiPack = CatcherHelper.statusBarNotifications[position].PackageName;
                        notificationSlave.CancelNotification(notiPack, notiTag, notiId);
                    }
                    else
                    {
                        notificationSlave.CancelNotification(CatcherHelper.statusBarNotifications[position].Key);
                    }
                }
                floatingNotificationView.Visibility = ViewStates.Gone;
            }
        }
        private void NotificationAdapterViewHolder_ItemClicked(object sender, Notificaciones.NotificationEventArgs.NotificationItemClickedEventArgs e)
        {
            using (OpenNotification notification = new OpenNotification(e.Position))
            {
                position = e.Position;
                floatingNotificationAppName.Text = notification.GetAppName();
                floatingNotificationWhen.Text = notification.GetWhen();
                floatingNotificationTitle.Text = notification.GetTitle();
                floatingNotificationText.Text = notification.GetText();
                if (floatingNotificationView.Visibility != ViewStates.Visible)
                {
                    floatingNotificationView.Visibility = ViewStates.Visible;
                }
                else
                {
                    floatingNotificationView.Visibility = ViewStates.Invisible;
                }
            };
        }

        private void FloatingNotificationView_Click(object sender, EventArgs e)
        {
            OpenNotification.ClickNotification(position);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            floatingNotificationView.Click -= FloatingNotificationView_Click;
            NotificationAdapterViewHolder.ItemClicked -= NotificationAdapterViewHolder_ItemClicked;
            NotificationAdapterViewHolder.ItemLongClicked-=NotificationAdapterViewHolder_ItemLongClicked;
            if (floatingNotificationView != null)
            {
                floatingNotificationAppName.Dispose();
                floatingNotificationWhen.Dispose();
                floatingNotificationTitle.Dispose();
                floatingNotificationText.Dispose();
                windowManager.RemoveView(floatingNotificationView);
                windowManager.Dispose();
            }
        }
    }
}