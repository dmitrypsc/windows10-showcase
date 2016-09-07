// Created by Kay Czarnotta on 22.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.System.Profile;

namespace SensorbergShowcase.Utils
{
    public static class Util
    {
        public static bool IsIoT
        {
            get { return AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.IoT"; }
        }

        public static bool IsMobile
        {
            get { return AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile"; }
        }

        public static

        bool IsDesktop
        {
            get { return AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"; }
        }
    }
}