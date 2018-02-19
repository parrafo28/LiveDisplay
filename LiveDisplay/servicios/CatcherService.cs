﻿using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Service.Notification;
using Android.Util;
using Android.Widget;
using Java.IO;
using LiveDisplay.Databases;
using LiveDisplay.Objects;
using System.IO;
using static Android.Graphics.Bitmap;

namespace LiveDisplay.Servicios
{
    [Service(Label = "Catcher", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE")]
    [IntentFilter(new[] { "android.service.notification.NotificationListenerService"})]
    internal class Catcher : NotificationListenerService
    {
        DBHelper helper = new DBHelper();
        public delegate void OnNotificationPostedEventHandler(StatusBarNotification statusBarNotification);

        //Válido para Lollipop en Adelante, no KitKat.
        public override void OnListenerConnected()
        {
            base.OnListenerConnected();
        }

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            ClsNotification notification = new ClsNotification
            {
                Id = sbn.Id,
                Titulo = sbn.Notification.Extras.GetCharSequence("android.title").ToString(),
                Texto = sbn.Notification.Extras.GetCharSequence("android.text").ToString(),
                Icono = int.Parse(sbn.Notification.Extras.Get(Notification.ExtraSmallIcon).ToString()),
                Paquete = sbn.PackageName
                
            };
            helper.InsertIntoTableNotification(notification);
            Log.Info("Imserción", "Registro Insertado desde OnNoificationPosted");
        }

        public override void OnNotificationRemoved(StatusBarNotification sbn)
        {
            ClsNotification notification = new ClsNotification
            {
                Id = sbn.Id,
                Titulo = null,
                Texto = null,
                Icono = 0
            };
            helper.DeleteTableNotification(notification);
            Log.Info("leeel", "Notificación Removida");
        }
        public byte[] BitmapToByteArray(Bitmap bitmap)
        {
            byte[] bitmapData;
            using (var stream = new MemoryStream())
            {
                bitmap.Compress(CompressFormat.Png, 0, stream);
                return bitmapData = stream.ToArray();
            }
                
        }
    }
}