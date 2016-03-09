/*
 * Adapted from VideoEffect project by Matthieu Maitre
 * https://github.com/mmaitre314/VideoEffect
 *
 * Licensed under Apache License 2.0.
 * See https://github.com/mmaitre314/VideoEffect/blob/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Windows.Media.Devices;
using System.Linq;
using System.Diagnostics;

namespace SensorbergShowcase.Utils
{
    class ContinuousAutoFocus : IDisposable
    {
        Stopwatch m_timeSinceLastBarcodeFound = Stopwatch.StartNew();
        FocusControl m_control;

        public static async Task<ContinuousAutoFocus> StartAsync(FocusControl control)
        {
            var autoFocus = new ContinuousAutoFocus(control);

            if (control.SupportedPresets.Contains(FocusPreset.Auto))
            {
                await control.SetPresetAsync(FocusPreset.Auto, /*completeBeforeFocus*/true);
            }

            return autoFocus;
        }

        ContinuousAutoFocus(FocusControl control)
        {
            m_control = control;
        }

        public void Dispose()
        {
            lock (this)
            {
                m_control = null;
            }
        }

        // Simulated continuous auto-focus
        async Task DriveAutoFocusAsync()
        {
            while (true)
            {
                FocusControl control;
                bool runFocusSweep;
                lock (this)
                {
                    if (m_control == null)
                    {
                        // Object was disposed
                        return;
                    }
                    control = m_control;
                    runFocusSweep = m_timeSinceLastBarcodeFound.ElapsedMilliseconds > 1000; 
                }

                if (runFocusSweep)
                {
                    try
                    {
                        await control.FocusAsync();
                    }
                    catch
                    {
                        // Failing to focus is ok (happens when preview lacks texture)
                    }
                }

                await Task.Delay(1000);
            }
        }
    }
}
