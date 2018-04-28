﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Service.Notification;
using Android.Util;
using LiveDisplay.Adapters;
using LiveDisplay.BroadcastReceivers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiveDisplay.Servicios
{
    [Service(Label = "Catcher", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE")]
    [IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
    internal class Catcher : NotificationListenerService
    {
        public static NotificationAdapter adapter;
        public static List<StatusBarNotification> listaNotificaciones;
        public static Catcher catcherInstance;
        private bool isListenerConnected = false;

        public override IBinder OnBind(Intent intent)
        {
            //KitKat Workaround: Enviar una notificación para poder iniciar la lista de notificaciones y obtener las notificaciones que hayan sido posteadas desde antes.
            //Porque parece imposible hacerlo sin otros métodos

            NotificationSlave slave = new NotificationSlave();
            ThreadPool.QueueUserWorkItem(o =>
            {
                Thread.Sleep(700);
                slave.PostNotification();
            });

            return base.OnBind(intent);
        }

        //Válido para Lollipop en Adelante, no KitKat.
        public override void OnListenerConnected()
        {
            base.OnListenerConnected();
            catcherInstance = this;
            listaNotificaciones = GetActiveNotifications().ToList();
            adapter = new NotificationAdapter(listaNotificaciones);
            RegisterReceiver(new ScreenOnOffReceiver(), new IntentFilter(Intent.ActionScreenOn));
            isListenerConnected = true;
            Log.Info("Listener connected, list: ", listaNotificaciones.Count.ToString());
        }

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop && isListenerConnected == false)
            {
                catcherInstance = this;
                listaNotificaciones = GetActiveNotifications().ToList();
                adapter = new NotificationAdapter(listaNotificaciones);
                RegisterReceiver(new ScreenOnOffReceiver(), new IntentFilter(Intent.ActionScreenOn));
                isListenerConnected = true;
                Log.Info("KitkatListener connected, list: ", listaNotificaciones.Count.ToString());
            }

            int id = sbn.Id;
            //Si encuentra el indice significa que el id ya existe y por lo tanto la notificación debe ser actualizada
            int indice = listaNotificaciones.IndexOf(listaNotificaciones.FirstOrDefault(o => o.Id == sbn.Id && o.PackageName == sbn.PackageName));
            //Condicional debido a que Play Store causa que algun item se pierda #wontfix ?
            if (indice >= 0)
            {
                listaNotificaciones.RemoveAt(indice);
                listaNotificaciones.Add(sbn);
                if (LockScreenActivity.lockScreenInstance != null)
                {
                    LockScreenActivity.lockScreenInstance.RunOnUiThread(() => adapter.NotifyItemChanged(indice));
                    LockScreenActivity.lockScreenInstance.RunOnUiThread(() => LockScreenActivity.lockScreenInstance.OnNotificationUpdated());
                }

                Log.Info("Elemento actualizado", "Tamaño lista: " + listaNotificaciones.Count);
            }
            else //No se encontró el indice y debe ser agregado a la lista
            {
                listaNotificaciones.Add(sbn);
                if (LockScreenActivity.lockScreenInstance != null)
                {
                    LockScreenActivity.lockScreenInstance.RunOnUiThread(() => adapter.NotifyItemInserted(listaNotificaciones.Count));
                }
                Log.Info("Elemento insertado", "Tamaño lista: " + listaNotificaciones.Count);
            }
        }

        public override void OnNotificationRemoved(StatusBarNotification sbn)
        {
            int indice = listaNotificaciones.IndexOf(listaNotificaciones.FirstOrDefault(o => o.Id == sbn.Id));
            if (indice >= 0)
            {
                listaNotificaciones.RemoveAt(indice);
            }
            if (adapter != null && LockScreenActivity.lockScreenInstance != null)
            {
                LockScreenActivity.lockScreenInstance.RunOnUiThread(() => adapter.NotifyItemRemoved(indice));
                Log.Info("Remoción, tamaño lista:  ", listaNotificaciones.Count.ToString());
            }
        }

        public override void OnListenerDisconnected()
        {
            base.OnListenerDisconnected();
            //Implementame
        }
    }
}