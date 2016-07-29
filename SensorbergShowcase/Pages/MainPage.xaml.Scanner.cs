using SensorbergSDK;
using SensorbergShowcase.Model;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MetroLog;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;

namespace SensorbergShowcase.Pages
{
    /// <summary>
    /// Contains the scanner specific UI implementation.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const double DefaultBeaconDetailsControlWidth = 350d;
        private IAsyncOperation<IUICommand> _bluetoothNotOnDialogOperation;

        /// <summary>
        /// Should be called in the constructor of this page. Handles the scanner specific construction.
        /// </summary>
        private void MainPageScanner()
        {
            Model.BeaconModel = new BeaconDetailsModel();
            UpdateBeaconCountInHeader(0);
        }

        /// <summary>
        /// Hooks/unhooks the scanner specific events. Does nothing, if already hooked/unhooked.
        /// </summary>
        /// <param name="hook">If true, will hook the events. If false, will unhook.</param>
        private void SetScannerSpecificEvents(bool hook)
        {
            Logger.Debug("MainPage.SetScannerSpecificEvents: " + Model.HaveScannerSpecificEventsBeenHooked + " -> " + hook);
            if (Model.HaveScannerSpecificEventsBeenHooked != hook)
            {
                IBeaconScanner scanner = _sdkManager.Scanner;

                if (hook)
                {
                    scanner.BeaconEvent += OnBeaconEventAsync;
                    scanner.BeaconNotSeenForAWhile += OnBeaconNotSeenForAWhileAsync;
                    Model.BeaconModel.BeaconDetailsCollection.CollectionChanged += OnBeaconDetailsCollectionChanged;
                }
                else
                {
                    scanner.BeaconEvent -= OnBeaconEventAsync;
                    scanner.BeaconNotSeenForAWhile -= OnBeaconNotSeenForAWhileAsync;
                    Model.BeaconModel.BeaconDetailsCollection.CollectionChanged -= OnBeaconDetailsCollectionChanged;
                }

                Model.HaveScannerSpecificEventsBeenHooked = hook;
            }
        }

        /// <summary>
        /// Sets the properties (checked, label) of the toggle scanner button based on the given scanner state.
        /// </summary>
        /// <param name="isScannerRunning">Should be true, if the scanner is running. False otherwise.</param>
        private void SetToggleScannerButtonProperties(bool isScannerRunning)
        {
            if (isScannerRunning)
            {
                toggleScanButton.Label = App.ResourceLoader.GetString("stopScanner/Label");
            }
            else
            {
                toggleScanButton.Label = App.ResourceLoader.GetString("startScanner/Label");
            }

        }

        /// <summary>
        /// Updates the string for the header based on the given beacon count.
        /// </summary>
        /// <param name="beaconCount">The number of beacons currently around.</param>
        private void UpdateBeaconCountInHeader(int beaconCount)
        {
            string scannerWithBeaconCountText = App.ResourceLoader.GetString("scannerWithBeaconCount/Text");
            Model.HeaderWithBeaconCount = string.Format(scannerWithBeaconCountText, beaconCount);
        }

        private void OnBeaconDetailsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<BeaconDetailsItem> collection = sender as ObservableCollection<BeaconDetailsItem>;
            int beaconCount = (collection != null) ? collection.Count : 0;
            UpdateBeaconCountInHeader(beaconCount);
        }

        private async void OnBeaconEventAsync(object sender, BeaconEventArgs eventArgs)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    Beacon beacon = eventArgs.Beacon;

                    if (eventArgs.EventType != BeaconEventType.None)
                    {
                        Logger.Debug("MainPage.OnBeaconEventAsync: '" + eventArgs.EventType + "' event from " + beacon);
                    }

                    bool isExistingBeacon = false;

                    if (Model.BeaconModel.Contains(beacon))
                    {
                        if (eventArgs.EventType == BeaconEventType.Exit)
                        {
                            Model.BeaconModel.Remove(beacon);
                        }
                        else 
                        {
                            Model.BeaconModel.AddOrReplace(beacon);
                        }

                        Model.BeaconModel.SortBeaconsBasedOnDistance();
                        isExistingBeacon = true;
                    }


                    if (!isExistingBeacon)
                    {
                        Model.BeaconModel.AddOrReplace(beacon);
                        Model.BeaconModel.SortBeaconsBasedOnDistance();
                    }

                    if (Model.BeaconModel.Count() > 0)
                    {
                        Model.BeaconsInRange = true;
                    }
                    else
                    {
                        Model.BeaconsInRange = false;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error while add/update beacon", e);
                }
            });
        }

        private async void OnBeaconNotSeenForAWhileAsync(object sender, Beacon e)
        {
            Logger.Debug("BeaconNotSeenForAWhileAsync {0}", e);
               await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Model.BeaconModel.SetBeaconRange(e, 0);
            });
        }

        private async void OnToggleScanButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (_sdkManager.IsScannerStarted)
                {
                    _sdkManager.StopScanner();
                }
                else
                {
                    SetScannerSpecificEvents(true); // Make sure the events are hooked
                    _sdkManager.StartScanner();
                }
            });
        }

        private async void OnScannerStatusChangedAsync(object sender, ScannerStatus e)
        {
            Logger.Debug("MainPage.OnScannerStatusChangedAsync: " + e);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                Model.IsScanning = e == ScannerStatus.Started;
                //SetToggleScannerButtonProperties(Model.IsScanning.Value);

                switch (e)
                {
                    case ScannerStatus.Stopped:
                        // Uncomment the following to unhook the scanner events when stopped
                        //SetScannerSpecificEvents(false);
                        break;

                    case ScannerStatus.Started:
                        if (_bluetoothNotOnDialogOperation != null)
                        {
                            _bluetoothNotOnDialogOperation.Cancel();
                            _bluetoothNotOnDialogOperation = null;
                        }

                        break;

                    case ScannerStatus.Aborted:
                        if (_bluetoothNotOnDialogOperation == null)
                        {
                            MessageDialog messageDialog = new MessageDialog(
                                App.ResourceLoader.GetString("enableBluetoothPrompt/Text"),
                                App.ResourceLoader.GetString("failedToStartWatcher/Text"));

                            messageDialog.Commands.Add(new UICommand(App.ResourceLoader.GetString("yes/Text"),
                                new UICommandInvokedHandler((command) =>
                                {
                                    _sdkManager.LaunchBluetoothSettingsAsync();
                                })));

                            messageDialog.Commands.Add(new UICommand(App.ResourceLoader.GetString("no/Text"),
                                new UICommandInvokedHandler((command) =>
                                {
                                    _sdkManager.StopScanner();
                                    _bluetoothNotOnDialogOperation = null;
                                })));

                            await messageDialog.ShowAsync();
                        }

                        break;
                }
            });
        }
    }
}
