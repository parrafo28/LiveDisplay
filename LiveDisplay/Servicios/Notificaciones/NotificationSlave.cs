﻿using Android.App;
using System;

namespace LiveDisplay.Servicios
{
    //Esta clase sirve para manipular las notificaciones, como quitarlas o agregarlas.
    internal class NotificationSlave
    {
        //Postear Notificaciones sobre mi app.
        private NotificationManager notificationManager = (NotificationManager)Application.Context.GetSystemService("notification");

        //Instancia a Catcher para cancelar notificaciones desde la lockScreen
        public void CancelNotification(string notiPack, string notiTag, int notiId)
        {
            //Deprecated since Android Kitkat Watch.
            Catcher.catcherInstance.CancelNotification(notiPack, notiTag, notiId);
        }

        public void CancelNotification(string key)
        {
            Catcher.catcherInstance.CancelNotification(key);
        }

        public void CancelAll()
        {
            Catcher.catcherInstance.CancelAllNotifications();
        }

        public void PostNotification()
        {
            //Test.
            Notification.Builder builder = new Notification.Builder(Application.Context);
            builder.SetContentTitle("LiveDisplay");
            builder.SetContentText("This is a test notification");
            builder.SetAutoCancel(true);
            builder.SetPriority(Convert.ToInt32(Android.App.NotificationPriority.Low));
            builder.SetSmallIcon(Resource.Drawable.ic_stat_default_appicon);
            notificationManager.Notify(1, builder.Build());
        }
    }
}