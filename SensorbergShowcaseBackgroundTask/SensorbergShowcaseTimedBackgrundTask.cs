// Created by Kay Czarnotta on 11.04.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.ApplicationModel.Background;
using SensorbergSDK.Background;

namespace SensorbergShowcaseBackgroundTask
{
    public sealed class SensorbergShowcaseTimedBackgrundTask : IBackgroundTask
    {
        private TimedBackgroundWorker TimedBackgroundWorker { get; set; }

        public SensorbergShowcaseTimedBackgrundTask()
        {
            TimedBackgroundWorker = new TimedBackgroundWorker();
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            TimedBackgroundWorker.Run(taskInstance);
        }
    }
}