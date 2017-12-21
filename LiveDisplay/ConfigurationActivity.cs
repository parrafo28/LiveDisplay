﻿
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace LiveDisplay
{
    [Activity(Label ="@string/app_title", MainLauncher = true, Icon ="@mipmap/ic_launcher")]
    class ConfigurationActivity: Activity
    {

        //Android Lifecycle.
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Configuracion);
            Switch swEnableAwake = FindViewById<Switch>(Resource.Id.swEnableAwake);
            //O es para Objeto, e es para Evento.
            swEnableAwake.Click += (o, e) =>
            {
                if (swEnableAwake.Checked == true)
                {
                    StartService(new Intent(this, typeof(AwakeService)));
                    
                }
                else if (swEnableAwake.Checked == false)
                {
                    StopService(new Intent(this, typeof(AwakeService)));
                }
            };
            
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            var inflater = MenuInflater;
            inflater.Inflate(Resource.Menu.menu_config, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.preview:
                    {
                        Intent intent = new Intent(this, typeof(LockScreenActivity));
                        StartActivity(intent);
                        return true;
                    }

                case Resource.Id.notificationSettings:
                    {
                        string lel = Android.Provider.Settings.ActionNotificationListenerSettings;
                        StartActivity(new Intent(lel));
                        return true;
                    }
            }
            return true;
        }

        protected override void OnPause()
        {
            base.OnPause();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        
    }
}