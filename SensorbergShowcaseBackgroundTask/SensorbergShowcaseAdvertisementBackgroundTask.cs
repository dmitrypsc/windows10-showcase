using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using SensorbergSDKBackground;

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
