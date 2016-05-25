// Created by Kay Czarnotta on 11.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.ApplicationModel.Background;
using SensorbergSDK.Background;

namespace SensorbergShowcaseBackgroundTask
{
    public sealed class SensorbergShowcaseAdvertisementBackgroundTask : IBackgroundTask
    {
        private AdvertisementWatcherBackgroundWorker BackgroundWorker { get; }

        public SensorbergShowcaseAdvertisementBackgroundTask()
        {
            BackgroundWorker = new AdvertisementWatcherBackgroundWorker();
        }
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundWorker.Run(taskInstance);
        }
    }
}
