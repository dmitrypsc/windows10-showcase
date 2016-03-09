using SensorbergSDK;
using SensorbergShowcase.Model;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SensorbergShowcase.Pages
{
    /// <summary>
    /// Contains the scanner specific UI implementation.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const double DefaultBeaconDetailsControlWidth = 350d;
        private IAsyncOperation<IUICommand> _bluetoothNotOnDialogOperation;

        public BeaconDetailsModel BeaconModel
        {
            get
            {
                return (BeaconDetailsModel)GetValue(BeaconModelProperty);
            }
            private set
            {
                SetValue(BeaconModelProperty, value);
            }
        }
        public static readonly DependencyProperty BeaconModelProperty =
            DependencyProperty.Register("BeaconModel", typeof(BeaconDetailsModel), typeof(MainPage),
                new PropertyMetadata(null));

        public double BeaconDetailsControlWidth
        {
            get
            {
                return (double)GetValue(BeaconDetailsControlWidthProperty);
            }
            private set
            {
                SetValue(BeaconDetailsControlWidthProperty, value);
            }
        }
        public static readonly DependencyProperty BeaconDetailsControlWidthProperty =
            DependencyProperty.Register("BeaconDetailsControlWidth", typeof(double), typeof(MainPage),
                new PropertyMetadata(DefaultBeaconDetailsControlWidth));
 
        public bool HaveScannerSpecificEventsBeenHooked
        {
            get;
            private set;
        }

        public bool IsScanning
        {
            get
            {
                return (bool)GetValue(IsScanningProperty);
            }
            private set
            {
                SetValue(IsScanningProperty, value);
            }
        }
        public static readonly DependencyProperty IsScanningProperty =
            DependencyProperty.Register("IsScanning", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        public bool BeaconsInRange
        {
            get
            {
                return (bool)GetValue(BeaconsInRangeProperty);
            }
            private set
            {
                SetValue(BeaconsInRangeProperty, value);
            }
        }
        public static readonly DependencyProperty BeaconsInRangeProperty =
            DependencyProperty.Register("BeaconsInRange", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        public string HeaderWithBeaconCount
        {
            get
            {
                return (string)GetValue(HeaderWithBeaconCountProperty);
            }
            private set
            {
                SetValue(HeaderWithBeaconCountProperty, value);
            }
        }
        public static readonly DependencyProperty HeaderWithBeaconCountProperty =
            DependencyProperty.Register("HeaderWithBeaconCount", typeof(string), typeof(MainPage),
                new PropertyMetadata(null));

        /// <summary>
        /// Should be called in the constructor of this page. Handles the scanner specific construction.
        /// </summary>
        private void MainPageScanner()
        {
            BeaconModel = new BeaconDetailsModel();
            UpdateBeaconCountInHeader(0);
        }

        /// <summary>
        /// Hooks/unhooks the scanner specific events. Does nothing, if already hooked/unhooked.
        /// </summary>
        /// <param name="hook">If true, will hook the events. If false, will unhook.</param>
        private void SetScannerSpecificEvents(bool hook)
        {
            if (HaveScannerSpecificEventsBeenHooked != hook)
            {
                System.Diagnostics.Debug.WriteLine("MainPage.SetScannerSpecificEvents: " + HaveScannerSpecificEventsBeenHooked + " -> " + hook);
                Scanner scanner = _sdkManager.Scanner;

                if (hook)
                {
                    scanner.BeaconEvent += OnBeaconEventAsync;
                    scanner.BeaconNotSeenForAWhile += OnBeaconNotSeenForAWhileAsync;
                    BeaconModel.BeaconDetailsCollection.CollectionChanged += OnBeaconDetailsCollectionChanged;
                }
                else
                {
                    scanner.BeaconEvent -= OnBeaconEventAsync;
                    scanner.BeaconNotSeenForAWhile -= OnBeaconNotSeenForAWhileAsync;
                    BeaconModel.BeaconDetailsCollection.CollectionChanged -= OnBeaconDetailsCollectionChanged;
                }

                HaveScannerSpecificEventsBeenHooked = hook;
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
                toggleScanButton.IsChecked = true;
                toggleScanButton.Label = App.ResourceLoader.GetString("stopScanner/Label");
            }
            else
            {
                toggleScanButton.IsChecked = false;
                toggleScanButton.Label = App.ResourceLoader.GetString("startScanner/Label");
            }

            toggleScanButton.IsEnabled = true;
        }

        /// <summary>
        /// Updates the string for the header based on the given beacon count.
        /// </summary>
        /// <param name="beaconCount">The number of beacons currently around.</param>
        private void UpdateBeaconCountInHeader(int beaconCount)
        {
            string scannerWithBeaconCountText = App.ResourceLoader.GetString("scannerWithBeaconCount/Text");
            HeaderWithBeaconCount = string.Format(scannerWithBeaconCountText, beaconCount);
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
                Beacon beacon = eventArgs.Beacon;

                if (eventArgs.EventType != BeaconEventType.None)
                {
                    System.Diagnostics.Debug.WriteLine("MainPage.OnBeaconEventAsync: '"
                        + eventArgs.EventType + "' event from " + beacon.ToString());
                }

                bool isExistingBeacon = false;

                if (BeaconModel.Contains(beacon))
                {
                    if (eventArgs.EventType == BeaconEventType.Exit)
                    {
                        BeaconModel.Remove(beacon);
                    }
                    else
                    {
                        BeaconModel.AddOrReplace(beacon);
                    }

                    BeaconModel.SortBeaconsBasedOnDistance();
                    isExistingBeacon = true;
                }


                if (!isExistingBeacon)
                {
                    BeaconModel.AddOrReplace(beacon);
                    BeaconModel.SortBeaconsBasedOnDistance();
                }

                if (BeaconModel.Count() > 0)
                {
                    BeaconsInRange = true;
                }
                else
                {
                    BeaconsInRange = false;
                }
            });
        }

        private async void OnBeaconNotSeenForAWhileAsync(object sender, Beacon e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                BeaconModel.SetBeaconRange(e, 0);
            });
        }

        private async void OnToggleScanButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                toggleScanButton.IsEnabled = false;

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
            System.Diagnostics.Debug.WriteLine("MainPage.OnScannerStatusChangedAsync: " + e);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IsScanning = (e == ScannerStatus.Started);
                SetToggleScannerButtonProperties(IsScanning);

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

                            _bluetoothNotOnDialogOperation = messageDialog.ShowAsync();
                        }

                        break;
                }
            });
        }
    }
}
