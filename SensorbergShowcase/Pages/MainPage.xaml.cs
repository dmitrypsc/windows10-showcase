using SensorbergSDK;
using SensorbergShowcase.Utils;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Email;
using Windows.Foundation;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MetroLog;
using Newtonsoft.Json;
using SensorbergControlLibrary.Controls;
using SensorbergControlLibrary.Model;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergShowcase.Model;

namespace SensorbergShowcase.Pages
{
    /// <summary>
    /// Construction and navigation related main page implementation reside here.
    /// We also manage the global main page components here such as the dialog
    /// control.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<MainPage>();
        private const int SettingsPivotIndex = 2;

        private bool _appIsOnForeground;
        public MainPageModel Model { get; } = new MainPageModel();
        public ICommand SendLogsCommand { get; } = new SendLogsCommand();
        private ScannerControl scannerControl;
        private IAsyncOperation<IUICommand> _bluetoothNotOnDialogOperation;

        private const ushort MANUFACTURER_ID = 0x004c;
        private const ushort BEACON_CODE = 0x0215;

        public const string TIMER_CLASS_NAME = "SensorbergShowcase.TimedBackgroundTask";
        public const string ADVERTISEMENT_CLASS_NAME = "SensorbergShowcase.AdvertisementWatcherBackgroundTask";


        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (hub != null && scannerControl == null)
            {
                scannerControl = FindChildControl<ScannerControl>(BeaconSection, "ScannerControl");
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (Model.SdkManager == null)
            {
                Model.SdkManager = SDKManager.Instance();
                SettingsControl_OnApiKeyChanged(Model.ApiKey);
            }
            UpdateBeaconCountInHeader(0);
            Model.SdkManager.BeaconActionResolved += OnBeaconActionResolved;
            Model.SdkManager.ScannerStatusChanged += OnScannerStatusChangedAsync;
            Model.SdkManager.Scanner.BeaconEvent += OnBeaconEventAsync;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Model.SdkManager.BeaconActionResolved -= OnBeaconActionResolved;
            Model.SdkManager.ScannerStatusChanged -= OnScannerStatusChangedAsync;
            Model.SdkManager.Scanner.BeaconEvent -= OnBeaconEventAsync;
        }

        private async void OnBeaconActionResolved(object sender, BeaconAction e)
        {
            Logger.Debug("Beacon action resolved" + e.Body + ", " + e.Payload);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Model.ActionResolved(e));
        }

        private async void SettingsControl_OnApiKeyChanged(string apiKey)
        {
            Model.ApiKey = apiKey;

            Model.SdkManager.Deinitialize(false);
            await Model.SdkManager.InitializeAsync(new SdkConfiguration()
            {
                ApiKey = Model.ApiKey,
                BackgroundTimerClassName = TIMER_CLASS_NAME,
                BackgroundAdvertisementClassName = ADVERTISEMENT_CLASS_NAME,
                BeaconCode = BEACON_CODE,
                ManufacturerId = MANUFACTURER_ID,
                AutoStartScanner = true
            });
        }
        private void OnScannerStatusChangedAsync(object sender, ScannerStatus e)
        {
            if (e == ScannerStatus.Aborted && _bluetoothNotOnDialogOperation == null)
            {
                if (Util.IsIoT)
                {
                    return;
                }
                MessageDialog messageDialog = new MessageDialog(
                    "Do you wish to enable Bluetooth on this device?",
                    "Failed to start Bluetooth LE advertisement watcher");

                messageDialog.Commands.Add(new UICommand("Yes",
                    command =>
                    {
                        Model.SdkManager.LaunchBluetoothSettingsAsync();
                    }));

                messageDialog.Commands.Add(new UICommand("No",
                    command =>
                    {
                        Model.SdkManager.StopScanner();
                    }));

                _bluetoothNotOnDialogOperation = messageDialog.ShowAsync();
            }
        }
        private async void OnToggleScanButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (Model.SdkManager.IsScannerStarted)
                {
                    Model.SdkManager.StopScanner();
                }
                else
                {
                    Model.SdkManager.StartScanner();
                }
            });
        }
        private async void OnBeaconEventAsync(object sender, BeaconEventArgs eventArgs)
        {
            if (scannerControl != null)
            {
                await scannerControl.OnBeaconEvent(eventArgs);
            }
        }

        /// <summary>
        /// Creates and displays a message dialog containing the current status of the SDK and Bluetooth.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnCheckStatusButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            string statusAsContentString = App.ResourceLoader.GetString("bluetooth/Text") + ": ";

            if (await DeviceUtils.GetIsBluetoothSupportedAsync())
            {
                if (await DeviceUtils.GetIsBluetoothEnabledAsync())
                {
                    statusAsContentString += App.ResourceLoader.GetString("enabled/Text");
                }
                else
                {
                    statusAsContentString += App.ResourceLoader.GetString("disabled/Text");
                }
            }
            else
            {
                statusAsContentString += App.ResourceLoader.GetString("notSupported/Text");
            }

            statusAsContentString +=
                "\n" + App.ResourceLoader.GetString("apiKey/Text") + ": "
                + (Model.IsApiKeyValid ? App.ResourceLoader.GetString("valid/Text") : App.ResourceLoader.GetString("invalid/Text"))
                + "\n" + App.ResourceLoader.GetString("beaconLayout/Text") + ": "
                + (Model.IsLayoutValid ? App.ResourceLoader.GetString("valid/Text") : App.ResourceLoader.GetString("invalid/Text"));

            MessageDialog messageDialog = new MessageDialog(
                statusAsContentString,
                App.ResourceLoader.GetString("status/Text"));

            await messageDialog.ShowAsync();
        }
        private T FindChildControl<T>(DependencyObject control, string ctrlName) where T : DependencyObject
        {
            int childNumber = VisualTreeHelper.GetChildrenCount(control);
            for (int i = 0; i < childNumber; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(control, i);
                FrameworkElement fe = child as FrameworkElement;
                // Not a framework element or is null
                if (fe == null) return null;

                if (child is T && fe.Name == ctrlName)
                {
                    // Found the control so return
                    return child as T;
                }
                else
                {
                    // Not found it - search children
                    T nextLevel = FindChildControl<T>(child, ctrlName);
                    if (nextLevel != null)
                        return nextLevel;
                }
            }
            return null;
        }

        private async void OnBackgroundFiltersUpdatedAsync(object sender, EventArgs e)
        {
            MessageDialog messageDialog = new MessageDialog(
                App.ResourceLoader.GetString("backgroundTaskFiltersUpdated/Text"),
                App.ResourceLoader.GetString("updateSuccessful/Text"));
            await messageDialog.ShowAsync();
        }

        /// <summary>
        /// Called when the app is brought on foreground or put in background.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            _appIsOnForeground = e.Visible;
            Model.SdkManager.OnApplicationVisibilityChanged(sender, e);

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
    }
    public class SendLogsCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            var emailMessage = new EmailMessage();
            emailMessage.Subject = emailMessage.Body = "Logs from " + Package.Current.DisplayName;
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Stream s = await LogManagerFactory.DefaultLogManager.GetCompressedLogs();
                MemoryStream m = new MemoryStream();
                await s.CopyToAsync(m);
                byte[] b = m.ToArray();
                InMemoryRandomAccessStream mem = new InMemoryRandomAccessStream();
                await mem.WriteAsync(b.AsBuffer());

                emailMessage.Attachments.Add(new EmailAttachment("logs.zip", RandomAccessStreamReference.CreateFromStream(mem)));
                await EmailManager.ShowComposeNewEmailAsync(emailMessage);
            }
            else
            {
                await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
            }
        }
    }
}
