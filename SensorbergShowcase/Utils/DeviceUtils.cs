using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Radios;
using Windows.Graphics.Display;

namespace SensorbergShowcase.Utils
{
    public class DeviceUtils
    {
        public static async Task<bool> GetIsBluetoothSupportedAsync()
        {
            var radios = await Radio.GetRadiosAsync();
            return (radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth) != null);
        }

        public static async Task<bool> GetIsBluetoothEnabledAsync()
        {
            var radios = await Radio.GetRadiosAsync();
            var bluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
            return (bluetoothRadio != null && bluetoothRadio.State == RadioState.On);
        }

        /// <summary>
        /// Resolves the display size of the device running this app.
        /// </summary>
        /// <returns>The display size in inches or less than zero if unable to resolve.</returns>
        public static double ResolveDisplaySizeInInches()
        {
            double displaySize = -1d;

            DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
            double rawPixelsPerViewPixel = displayInformation.RawPixelsPerViewPixel;
            float rawDpiX = displayInformation.RawDpiX;
            float rawDpiY = displayInformation.RawDpiY;
            double screenResolutionX = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Bounds.Width * rawPixelsPerViewPixel;
            double screenResolutionY = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Bounds.Height * rawPixelsPerViewPixel;

            if (rawDpiX > 0 && rawDpiY > 0)
            {
                displaySize = Math.Sqrt(
                    Math.Pow(screenResolutionX / rawDpiX, 2) +
                    Math.Pow(screenResolutionY / rawDpiY, 2));
                displaySize = Math.Round(displaySize, 1); // One decimal is enough
            }

            return displaySize;
        }
    }
}
