﻿using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Service.Notification;
using LiveDisplay.BroadcastReceivers;
using LiveDisplay.Servicios.Music;
using LiveDisplay.Servicios.Notificaciones;
using LiveDisplay.Servicios.Notificaciones.NotificationEventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiveDisplay.Servicios
{
    [Service(Label = "Catcher", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE")]
    [IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
    internal class Catcher : NotificationListenerService, ISensorEventListener
    {
        //Move to respective fragments
        private BatteryReceiver batteryReceiver;
        private ScreenOnOffReceiver screenOnOffReceiver;

        //Manipular las sesiones-
        private MediaSessionManager mediaSessionManager;
        //el controlador actual de media.
        private ActiveMediaSessionsListener activeMediaSessionsListener;

#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
        private RemoteController remoteController;
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos
        private CatcherHelper catcherHelper;
        private List<StatusBarNotification> statusBarNotifications;
        private SensorManager sensorManager;
        private Sensor sensorAccelerometer;
#pragma warning disable CS0414 // El campo 'Catcher.isInAPlainSurface' está asignado pero su valor nunca se usa
        bool isInAPlainSurface = false;
#pragma warning restore CS0414 // El campo 'Catcher.isInAPlainSurface' está asignado pero su valor nunca se usa

        public override IBinder OnBind(Intent intent)
        {
            //Workaround for Kitkat to Retrieve Notifications.
            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                //Activate the remote controller in Kitkat, because is Deprecated since Lollipop

                ThreadPool.QueueUserWorkItem(o =>
                {
                    Thread.Sleep(1000);
                    RetrieveNotificationFromStatusBar();
                });

                SubscribeToEvents();
                RegisterReceivers();
                //TODO: This setting is sensible to user configuration and also the Inactive hours setting.
                StartWatchingDeviceMovement();
                //New remote controller for Kitkat

                //Fix me, I got disposed too early.

#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
                using (remoteController = new RemoteController(Application.Context, new MusicControllerKitkat()))
                {
                    remoteController.SetArtworkConfiguration(450, 450);
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos
                    var audioService = (AudioManager)Application.Context.GetSystemService(AudioService);
#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
                    audioService.RegisterRemoteController(remoteController);
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos
                }
            }
            return base.OnBind(intent);
        }

        public override void OnListenerConnected()
        {
            activeMediaSessionsListener = new ActiveMediaSessionsListener();
            //RemoteController Lollipop and Beyond Implementation
            mediaSessionManager = (MediaSessionManager)GetSystemService(MediaSessionService);

            //Listener para Sesiones
            using (var h = new Handler(Looper.MainLooper)) //Using UI Thread because seems to crash in some devices.
                h.Post(() => { mediaSessionManager.AddOnActiveSessionsChangedListener(activeMediaSessionsListener, new ComponentName(this, Java.Lang.Class.FromType(typeof(Catcher)))); });
            


            SubscribeToEvents();
            RegisterReceivers();
            RetrieveNotificationFromStatusBar();
            //TODO:This setting is sensible to user configuration and also the Inactive hours setting.
            StartWatchingDeviceMovement();
        }

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            catcherHelper.InsertNotification(sbn);

            base.OnNotificationPosted(sbn);
        }

        public override void OnNotificationRemoved(StatusBarNotification sbn)
        {
            catcherHelper.RemoveNotification(sbn);
            base.OnNotificationRemoved(sbn);
        }

        public override void OnListenerDisconnected()
        {
            catcherHelper.Dispose();
            mediaSessionManager.RemoveOnActiveSessionsChangedListener(activeMediaSessionsListener);
            UnregisterReceivers();
            base.OnListenerDisconnected();
        }

        public override bool OnUnbind(Intent intent)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                catcherHelper.Dispose();
                UnregisterReceivers();
            }

            return base.OnUnbind(intent);
        }

        private void RetrieveNotificationFromStatusBar()
        {
            statusBarNotifications = new List<StatusBarNotification>();
            foreach (var notification in GetActiveNotifications().ToList())
            {
                if (notification.IsOngoing==false||notification.IsClearable==true)
                {
                    statusBarNotifications.Add(notification);
                }
            }

            catcherHelper = new CatcherHelper(statusBarNotifications);
        }

        //Subscribe to events by NotificationSlave
        private void SubscribeToEvents()
        {
            NotificationSlave notificationSlave = NotificationSlave.NotificationSlaveInstance();
            notificationSlave.AllNotificationsCancelled += NotificationSlave_AllNotificationsCancelled;
            notificationSlave.NotificationCancelled += NotificationSlave_NotificationCancelled;
            notificationSlave.NotificationCancelledLollipop += NotificationSlave_NotificationCancelledLollipop;
        }
        private void RegisterReceivers()
        {
            using (IntentFilter intentFilter = new IntentFilter())
            {
                    screenOnOffReceiver = new ScreenOnOffReceiver();
                    intentFilter.AddAction(Intent.ActionScreenOff);
                    intentFilter.AddAction(Intent.ActionScreenOn);
                    RegisterReceiver(screenOnOffReceiver, intentFilter);
            }
            using (IntentFilter intentFilter = new IntentFilter())
            {
                batteryReceiver = new BatteryReceiver();
                intentFilter.AddAction(Intent.ActionBatteryChanged);
                RegisterReceiver(batteryReceiver, intentFilter);
            }
                        
        }

        //Events:
        private void NotificationSlave_NotificationCancelledLollipop(object sender, NotificationCancelledEventArgsLollipop e)
        {
            CancelNotification(e.Key);
        }

        private void NotificationSlave_NotificationCancelled(object sender, NotificationCancelledEventArgsKitkat e)
        {
#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
            CancelNotification(e.NotificationPackage, e.NotificationTag, e.NotificationId);
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos
        }

        private void NotificationSlave_AllNotificationsCancelled(object sender, EventArgs e)
        {
            CancelAllNotifications();
            catcherHelper.CancelAllNotifications();
        }

        //Sensor Implementation
        private void StartWatchingDeviceMovement()
        {
            sensorManager = (SensorManager)GetSystemService(SensorService);
            sensorAccelerometer = sensorManager.GetDefaultSensor(SensorType.Accelerometer);
            sensorManager.RegisterListener(this, sensorAccelerometer, SensorDelay.Normal);
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            //Nothing yet.
        }

        public void OnSensorChanged(SensorEvent e)
        {

            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                //Console.WriteLine("X= " + e.Values[0]);//Aceleración en el eje X- Gx;
                //Console.WriteLine("Y= " + e.Values[1]);//Aceleración en el eje Y- Gy;
                //Console.WriteLine("Z= " + e.Values[2]);//Aceleración en el eje Z- Gz;

                //TODO:

                //Detect phone on plain surface:
                //Z axis must have the following value:
                //>10 m/s2;
                //Y axis must be less than 3m/s2 so, the device can be slightly tilted and still being
                //in a Plain surface.

                //This will be true if above conditions match
                if (e.Values[2] > 10 && e.Values[1] < 3)
                {

                }

                //after, use this value to decide if wake up or not the screen.
                //We don't want to awake the screen if the device is already vertical for some reason.

                //Put a timer of 3 seconds, and if the device is still with these values,
                //the phone is left in a plain surface.
                //New feature? Don't awake phone on new Notification while phone is left alone
                //To avoid Unnecesary awake if the user won't see it.

                //Detect if User has grabbed the phone back up:
                //Z axis must be less than 10 m/s2("Example: 9.5") it means that Z  axis is not being
                //Accelerated and
                //Y axis must be greater than 3m/s2
                //if (ScreenOnOffReceiver.isScreenOn == false)//&& isInAPlainSurface=true
                //{
                //    if (e.Values[2] < 9.6f && e.Values[1] > 3)
                //    {
                //        //Awake the phone:
                //        Awake.WakeUpScreen();
                //        Awake.LockScreen();
                //    }
                //}

                //The less Z axis m/s2 value is, and the more Y axis m/s2 value is, the phone more vertically is.

                //Notes:
                //X axis is not necessary as I don't need to know if the phone is being moved Horizontally.
            }
        }

        private void UnregisterReceivers()
        {
            UnregisterReceiver(screenOnOffReceiver);
            UnregisterReceiver(batteryReceiver);
        }
    }
}