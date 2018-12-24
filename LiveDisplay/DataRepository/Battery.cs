﻿using LiveDisplay.Misc;

namespace LiveDisplay.DataRepository
{
    /// <summary>
    /// This class will handle the Battery status provided by the BroadcastReceiver and Notify ClockFragment about the changes in battery
    /// </summary>
    internal class Battery : Java.Lang.Object
    {
        private static Battery instance;

        public static int BatteryLevel { get; set; }
        public static BatteryLevelFlags BatteryLevelFlags { get; set; }

        private Battery()
        {
        }

        public static Battery BatteryInstance()
        {
            if (instance == null)
            {
                instance = new Battery();
            }
            return instance;
        }

        public static int ReturnBatteryLevel()
        {
            return BatteryLevel;
        }
    }
}